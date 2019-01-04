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
        [CommandMethod("XXSA")]
        public void StretchAlignEdge()
        {
            Document doc = GetDocument();
            Database db = doc.Database;
            Editor ed = doc.Editor;
            try
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    PromptEntityOptions peo = new PromptEntityOptions(
                        "\nSelect a base line or a polyline: ")
                    {
                        AllowNone = false
                    };
                    peo.SetRejectMessage("\n>>>Select either a line or a polyline: ");
                    peo.AddAllowedClass(typeof(Polyline), true);
                    peo.AddAllowedClass(typeof(Line), true);
                    PromptEntityResult per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK)
                        return;
                    var baseCurve = trans.GetObject(per.ObjectId, OpenMode.ForRead) as Curve;
                    var basePickPt = baseCurve.GetClosestPointTo(per.PickedPoint, true);
                    peo = new PromptEntityOptions("\nSelect a polyline: ")
                    {
                        AllowNone = false
                    };
                    peo.SetRejectMessage("\n>>>This is not a polyline, Select a polyline: ");
                    peo.AddAllowedClass(typeof(Polyline), true);
                    per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK)
                        return;
                    var pline = trans.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                    var pickPt = pline.GetClosestPointTo(per.PickedPoint, true);

                    var baseStartParam = (int)baseCurve.GetParameterAtPoint(basePickPt);
                    var baseEndParam = baseStartParam + 1;
                    var baseStartPt = baseCurve.GetPointAtParameter(baseStartParam);
                    var baseEndPt = baseCurve.GetPointAtParameter(baseEndParam);

                    double par = (int)pline.GetParameterAtPoint(pickPt);
                    double pre1 = par > 0 ? par - 1 : pline.EndParam - 1;
                    double pos1 = par + 1 == pline.EndParam ? 0 : par + 1;
                    double pos2 = pos1 == pline.EndParam ? 1 : pos1 + 1;

                    // get the the surrounding points
                    var p1 = pline.GetPointAtParameter(pre1);
                    var p2 = pline.GetPointAtParameter(par);
                    var p3 = pline.GetPointAtParameter(pos1);
                    var p4 = pline.GetPointAtParameter(pos2);

                    var pInt1 = Inters(p1, p2, baseStartPt, baseEndPt);
                    if (!pInt1.HasValue)
                        return;
                    var pInt2 = Inters(p3, p4, baseStartPt, baseEndPt);
                    if (!pInt2.HasValue)
                        return;
                    pline.UpgradeOpen();
                    pline.SetPointAt((int)par, pInt1.Value.GetPoint2d());
                    pline.SetPointAt((int)pos1, pInt2.Value.GetPoint2d());
                    trans.Commit();
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(ex.Message);
            }
            finally
            {

            }

        }

        public Point2d? Inters(Point2d p1, Point2d p2, Point2d p3, Point2d p4)
        {
            double a1 = p2.Y - p1.Y;
            double b1 = p1.X - p2.X;
            double c1 = b1 * p1.Y + a1 * p1.X;

            double a2 = p4.Y - p3.Y;
            double b2 = p3.X - p4.X;
            double c2 = b2 * p3.Y + a2 * p3.X;

            double det = a1 * b2 - a2 * b1;
            if (Math.Abs(det) < 0.000001)
                return null;
            double x = (c1 * b2 - c2 * b1) / det;
            double y = (c2 * a1 - c1 * a2) / det;
            return new Point2d(x, y);
        }

        public Point3d? Inters(Point3d p1, Point3d p2, Point3d p3, Point3d p4)
        {
            var r = Inters(p1.GetPoint2d(), p2.GetPoint2d(), p3.GetPoint2d(), p4.GetPoint2d());
            if (!r.HasValue)
                return null;
            return new Point3d(r.Value.X, r.Value.Y, 0.0);
        }
    }
}
