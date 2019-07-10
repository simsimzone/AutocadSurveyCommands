using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GI = Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(
  typeof(AutocadSurveyCommands.AutocadSurveyCommands)
)]


namespace AutocadSurveyCommands
{

    public partial class AutocadSurveyCommands
    {
        [CommandMethod("XXSE2")]
        public void StretchPolylineEdge_V2()
        {
            Document doc = GetDocument();
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ObjectId currentId = ObjectId.Null;
            int currentParam = -1;
            Polyline currentPolyline = null;
            DBObjectCollection transientColl = null;
            double requiredArea = 0.0;
            bool isNotEnoughArea = false;
            Point3d? currPoint = null;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    PromptDoubleOptions pdo = new PromptDoubleOptions(
                        "\nSpecify the required area: ")
                    {
                        AllowNegative = false,
                        AllowNone = false,
                        AllowZero = false,
                        DefaultValue = defaultArea,
                        UseDefaultValue = defaultArea == 0 ? false : true
                    };
                    PromptDoubleResult pdr = ed.GetDouble(pdo);
                    if (pdr.Status != PromptStatus.OK)
                        return;
                    requiredArea = defaultArea = pdr.Value;

                    ed.TurnForcedPickOn();
                    ed.PointMonitor += Ed_PointMonitor;
                    Polyline pline = SelectPolyline(ed, trans,
                        "\nSelect a closed polyline: ", "\n>>>Select a closed polyline: ", true,
                        out Point3d pickPt);
                    if (pline == null)
                        return;

                    var pts = GetStretchPoints(pline, pickPt, out int par);
                    if (pts == null)
                    {
                        ed.WriteMessage("\n not enough area...");
                        return;
                    }
                    pline.UpgradeOpen();
                    pline.SetPointAt(par, pts[1]);
                    pline.SetPointAt(pline.ParameterAfter(par), pts[2]);
                    trans.Commit();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    ed.WriteMessage(ex.Message + ex.StackTrace);
                }
                finally
                {
                    ed.PointMonitor -= Ed_PointMonitor;
                    if (currentPolyline != null)
                    {
                        EraseTransient();
                        currentPolyline = null;
                    }
                }
            }

            

            Polyline CreateTransient(Transaction tr, ObjectId id, PointMonitorEventArgs e
                ,out int par)
            {
                var rawPoint = e.Context.RawPoint;
                var pline = tr.GetObject(id, OpenMode.ForRead) as Polyline;
                var pickPt = pline.GetClosestPointTo(rawPoint, true);
                var res = GetStretchPoints(pline, pickPt, out par);
                
                transientColl = new DBObjectCollection();
                if (res != null)
                {
                    isNotEnoughArea = false;
                    Polyline drawable = new Polyline();
                    drawable.AddVertexAt(0, res[0], 0, 0, 0);
                    drawable.AddVertexAt(1, res[1], 0, 0, 0);
                    drawable.AddVertexAt(2, res[2], 0, 0, 0);
                    drawable.AddVertexAt(3, res[3], 0, 0, 0);

                    drawable.ColorIndex = 3;

                    transientColl.Add(drawable);
                }
                else
                {
                    isNotEnoughArea = true;
                    Point2d pixels = e.Context.DrawContext.Viewport.GetNumPixelsInUnitSquare(rawPoint);
                    int glyphSize = CustomObjectSnapMode.GlyphSize;
                    glyphHeight = glyphSize / pixels.Y * 1.0;

                    radius = glyphHeight / 2.0;
                    center = e.Context.RawPoint + new Vector3d(3 * radius, 3 * radius, 0);

                    Point2d p1 = (center + new Vector3d(-radius, -radius, 0)).GetPoint2d();
                    Point2d p2 = (center + new Vector3d(+radius, +radius, 0)).GetPoint2d();
                    Point2d p3 = (center + new Vector3d(-radius, +radius, 0)).GetPoint2d();
                    Point2d p4 = (center + new Vector3d(+radius, -radius, 0)).GetPoint2d();
                    Polyline pl1 = new Polyline
                    {
                        ColorIndex = 1
                    };
                    pl1.AddVertexAt(0, p1, 0, 0, 0);
                    pl1.AddVertexAt(1, p2, 0, 0, 0);


                    Polyline pl2 = new Polyline()
                    {
                        ColorIndex = 1
                    };
                    pl2.AddVertexAt(0, p3, 0, 0, 0);
                    pl2.AddVertexAt(1, p4, 0, 0, 0);

                    transientColl.Add(pl1);
                    transientColl.Add(pl2);
                }

                for (int i = 0; i < transientColl.Count; i++)
                    GI.TransientManager.CurrentTransientManager.AddTransient(
                            transientColl[i], GI.TransientDrawingMode.Contrast,
                            128, new IntegerCollection());
                return pline;
            }

            void UpdateTransients(Point3d lastPt, Point3d currPt)
            {
                if (transientColl == null)
                    return;
                if (isNotEnoughArea)
                {
                    Matrix3d mat = Matrix3d.Displacement(lastPt.GetVectorTo(currPt));
                    foreach (Entity e in transientColl)
                    {
                        e.TransformBy(mat);
                        GI.TransientManager.CurrentTransientManager.UpdateTransient(e,
                            new IntegerCollection());
                    }
                }
            }

            void EraseTransient()
            {
                if (transientColl == null)
                    return;
                GI.TransientManager.CurrentTransientManager.EraseTransients(
                    GI.TransientDrawingMode.Contrast,
                    128, new IntegerCollection());
                foreach (GI.Drawable drawable in transientColl)
                {
                    drawable.Dispose();
                }
                transientColl.Clear();
                transientColl = null;
            }

            
            void Ed_PointMonitor(object sender, PointMonitorEventArgs e)
            {
                try
                {
                    var fsPaths = e.Context.GetPickedEntities();
                    // nothing under the mouse cursor.
                    if (fsPaths == null || fsPaths.Length == 0)
                    {
                        if (currentPolyline != null)
                        {
                            EraseTransient();
                            currentPolyline = null;
                        }
                        return;
                    }

                    var rawPoint = e.Context.RawPoint;
                    var oIds = fsPaths[0].GetObjectIds();
                    var id = oIds[oIds.GetUpperBound(0)];

                    if (currPoint.HasValue)
                    {
                        UpdateTransients(currPoint.Value, rawPoint);
                    }
                    currPoint = rawPoint;

                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        var pline = tr.GetObject(id, OpenMode.ForRead) as Polyline;
                        var pickedPt = pline.GetClosestPointTo(rawPoint, true);
                        var par = (int)pline.GetParameterAtPoint(pickedPt);
                        if (currentPolyline != pline || currentParam != par)
                        {
                            EraseTransient();
                            currentPolyline = CreateTransient(tr, id, e, out currentParam);
                        }
                        //else if (currentPolyline == pline && currentParam == par)
                        //{

                        //}
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    ed.WriteMessage(ex.Message);
                }
            }

            Point2d[] GetStretchPoints(
            Polyline pline, Point3d pickPt, out int par)
            {
                var area = pline.GetArea();
                par = (int)pline.GetParameterAtPoint(pickPt);
                int pre1 = par > 0 ? par - 1 : (int)pline.EndParam - 1;
                int pos1 = par + 1 == (int)pline.EndParam ? 0 : par + 1;
                int pos2 = pos1 == (int)pline.EndParam ? 1 : pos1 + 1;
                // get the the surrounding points
                var p1 = pline.GetPointAtParameter(pre1).GetPoint2d();
                var p2 = pline.GetPointAtParameter(par).GetPoint2d();
                var p3 = pline.GetPointAtParameter(pos1).GetPoint2d();
                var p4 = pline.GetPointAtParameter(pos2).GetPoint2d();
                double l1 = p2.GetDistanceTo(p3);

                double dA = requiredArea - Math.Abs(area);
                double ang1 = p1.GetVectorTo(p2).Angle;
                double ang2 = p4.GetVectorTo(p3).Angle;
                double ang = p2.GetVectorTo(p3).Angle;
                double dAng1 = (area > 0) ? ang - ang1 : ang1 - ang;
                double dAng2 = (area > 0) ? ang - ang2 : ang2 - ang;
                double f = 0.5 * (1.0 / Math.Tan(dAng2) - 1.0 / Math.Tan(dAng1));
                double v = l1 * l1 + 4 * dA * f;
                if (v < 0)
                    return null;
                double h;
                if (Math.Abs(ang1 - ang2) < 0.00001)
                    h = dA / l1;
                else
                    h = (-l1 + Math.Sqrt(v)) / (2.0 * f);
                var pt2 = p2.Polar(ang1, h / Math.Sin(dAng1));
                var pt3 = p3.Polar(ang2, h / Math.Sin(dAng2));
                return new Point2d[] { p2, pt2, pt3, p3 };
            }
        }
    }
}
