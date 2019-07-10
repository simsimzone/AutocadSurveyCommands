using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(
  typeof(AutocadSurveyCommands.AutocadSurveyCommands)
)]

namespace AutocadSurveyCommands
{
    public partial class AutocadSurveyCommands
    {
        enum Operation { AREA, ALIGN, RIGHT_ANGLE };

        [CommandMethod("XXSV2")]
        public void StretchPolylineVertexAlign()
        {
            Document doc = GetDocument();
            Database db = doc.Database;
            Editor ed = doc.Editor;
            Operation operation = Operation.AREA;
            const string AREA = "ARea";
            const string ALIGN = "Align";
            const string RIGHT = "Right-angle";

            Point2d basePt1 = Point2d.Origin;
            Point2d basePt2 = Point2d.Origin;

            PromptEntityResult per;

            double requiredArea = 0;
            try
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    while (true)
                    {

                        if (operation == Operation.AREA)
                        {
                            PromptDoubleOptions pdo = new PromptDoubleOptions(
                                "\nSpecify the required area or: ")
                            {
                                AllowNegative = false,
                                AllowNone = false,
                                AllowZero = false,
                                DefaultValue = defaultArea,
                                UseDefaultValue = defaultArea == 0 ? false : true,
                                AppendKeywordsToMessage = true
                            };
                            pdo.Keywords.Add(ALIGN);
                            pdo.Keywords.Add(RIGHT);

                            PromptDoubleResult pdr = ed.GetDouble(pdo);
                            if (pdr.Status == PromptStatus.Keyword)
                            {
                                if (pdr.StringResult == ALIGN)
                                {
                                    ed.WriteMessage("\nYou have selected Align operation...");
                                    operation = Operation.ALIGN;
                                }
                                else if (pdr.StringResult == RIGHT)
                                {
                                    ed.WriteMessage("\nYou have selected Right angle operation...");
                                    operation = Operation.RIGHT_ANGLE;
                                }
                                continue;
                            }
                            else if (pdr.Status != PromptStatus.OK)
                                return;
                            else
                            {
                                requiredArea = defaultArea = pdr.Value;
                            }
                        }

                        if (operation == Operation.ALIGN)
                        {
                            // Ask the user to select a source line segment
                            string kw = PickLinePolyline(
                                trans, "\n Pick a source line segment or: "
                                , new List<string> { AREA, RIGHT }
                                , out Point3d? p1, out Point3d? p2);
                            // the user pressed cancel
                            if (p1 == null && kw == null)
                                return;
                            if (kw != null)
                            {
                                operation = kw == AREA ? Operation.AREA : Operation.RIGHT_ANGLE;
                                continue;
                            }
                            basePt1 = p1.Value.GetPoint2d();
                            basePt2 = p2.Value.GetPoint2d();
                        }

                        PromptEntityOptions peo = new PromptEntityOptions("\nSelect a polyline or: ")
                        {
                            AllowNone = false,
                            AppendKeywordsToMessage = true
                        };
                        peo.SetRejectMessage("\n>>>this is not a polyline, Select a polyline or: ");
                        peo.AddAllowedClass(typeof(Polyline), true);

                        //ed.TurnForcedPickOn();
                        //ed.PointMonitor += Ed_PointMonitor;

                        if (operation == Operation.ALIGN)
                        {
                            peo.Keywords.Add(AREA);
                            peo.Keywords.Add(RIGHT);
                        }
                        else if (operation == Operation.RIGHT_ANGLE)
                        {
                            peo.Keywords.Add(AREA);
                            peo.Keywords.Add(ALIGN);
                        }
                        else
                        {
                            peo.Keywords.Add(AREA);
                            peo.Keywords.Add(ALIGN);
                            peo.Keywords.Add(RIGHT);
                        }

                        per = ed.GetEntity(peo);
                        if (per.Status == PromptStatus.Keyword)
                        {
                            if (per.StringResult == AREA)
                            {
                                operation = Operation.AREA;
                            }
                            else if(per.StringResult == ALIGN)
                            {
                                operation = Operation.ALIGN;
                            }
                            else
                            {
                                operation = Operation.RIGHT_ANGLE;
                            }
                            continue;
                        }
                        if (per.Status != PromptStatus.OK)
                            return;
                        break;
                    }
                    var pline = trans.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                    // pline.Closed needs to be checked only for area option
                    if (!pline.Closed)
                    {
                        ed.WriteMessage("\nThe selected polyline is not closed");
                        return;
                    }
                    var area = pline.GetArea();
                    var pickedPt = pline.GetClosestPointTo(per.PickedPoint, true);

                    double param = pline.GetParameterAtPoint(pickedPt);
                    int mIndex, fIndex, eIndex;
                    int endParam = (int)pline.EndParam - 1;
                    if (param - Math.Truncate(param) < 0.5)
                    {
                        mIndex = (int)param;
                        fIndex = mIndex == endParam ? 0 : mIndex + 1;
                        eIndex = mIndex == 0 ? endParam : mIndex - 1;
                    }
                    else
                    {
                        mIndex = (int)param == endParam ? 0 : (int)param + 1;
                        fIndex = mIndex == 0 ? endParam : mIndex - 1;
                        eIndex = mIndex == endParam ? 0 : mIndex + 1;
                    }

                    var movablePoint = pline.GetPointAtParameter(mIndex).GetPoint2d();
                    var fixedPoint = pline.GetPointAtParameter(fIndex).GetPoint2d();
                    var extPoint = pline.GetPointAtParameter(eIndex).GetPoint2d();
                    Point2d finaltPoint;
                    var l1 = movablePoint.GetDistanceTo(fixedPoint);
                    if (operation == Operation.AREA)
                    {
                        
                        var dA = requiredArea - Math.Abs(area);
                        var ang1 = fixedPoint.GetVectorTo(movablePoint).Angle;
                        var ang2 = extPoint.GetVectorTo(movablePoint).Angle;

                        var ang = ang1 - ang2;
                        var dl = 2 * dA / (l1 * Math.Abs(Math.Sin(ang)));
                        finaltPoint = movablePoint.Polar(ang2, dl);
                    }
                    else if (operation == Operation.ALIGN)
                    {
                        var ang1 = movablePoint.AngleTo(fixedPoint);
                        var ang2 = extPoint.AngleTo(movablePoint);

                        var alpha = ang1 - ang2;
                        var theta = basePt1.AngleTo(basePt2);
                        var beta = Math.PI - theta + ang2;
                        var gamma = theta - alpha - ang2;

                        var dl = l1 * Math.Sin(gamma) / Math.Sin(beta);
                        finaltPoint = movablePoint.Polar(ang2, dl);
                    }
                    else
                    {
                        // right angle operation
                        var ang1 = movablePoint.AngleTo(fixedPoint);
                        var ang2 = extPoint.AngleTo(movablePoint);
                        var alpha = ang1 - ang2;
                        var dl = l1 * Math.Cos(alpha);
                        finaltPoint = movablePoint.Polar(ang2, dl);
                    }

                    pline.UpgradeOpen();
                    pline.SetPointAt(mIndex, finaltPoint);
                    trans.Commit();
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(ex.Message);
            }

            string PickLinePolyline
                (Transaction tr, string message, List<string> keywords,
                out Point3d? pt1, out Point3d? pt2)
            {
                pt1 = pt2 = null;
                PromptEntityOptions peo = new PromptEntityOptions(message)
                {
                    AllowNone = false
                };
                peo.SetRejectMessage(message);
                peo.AddAllowedClass(typeof(Line), true);
                peo.AddAllowedClass(typeof(Polyline), true);

                if (keywords != null && keywords.Count > 0)
                {
                    peo.AppendKeywordsToMessage = true;
                    for (int i = 0; i < keywords.Count; i++)
                    {
                        peo.Keywords.Add(keywords[i]);
                    }
                }
                PromptEntityResult perBase;
                Curve curve;
                Point3d pickedPt;
                
                while (true)
                {
                    perBase = ed.GetEntity(peo);
                    if (perBase.Status == PromptStatus.Cancel)
                        return null;
                    if (perBase.Status == PromptStatus.Keyword)
                    {
                        return perBase.StringResult;
                    }
                    curve = tr.GetObject(perBase.ObjectId, OpenMode.ForRead) as Curve;
                    if (perBase.Status == PromptStatus.OK
                        && curve != null)
                        break;
                }
                pickedPt = curve.GetClosestPointTo(perBase.PickedPoint, true);
                int par = (int)curve.GetParameterAtPoint(pickedPt);

                pt1 = curve.GetPointAtParameter(par);
                pt2 = curve.Point3dAfter(par);
                return null;
            }
        }

        //private void Ed_PointMonitor(object sender, PointMonitorEventArgs e)
        //{

        //}
    }
}
