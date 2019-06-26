using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(
  typeof(AutocadSurveyCommands.AutocadSurveyCommands)
)]

namespace AutocadSurveyCommands
{
    public partial class AutocadSurveyCommands
    {
        [CommandMethod("XXAA")]
        public void AdjustAlign()
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
                    var l = p2.DistanceTo(p3);

                    var alp1 = p1.GetAngleTo(p2);
                    var alp2 = p2.GetAngleTo(p3);
                    var alp3 = p3.GetAngleTo(p4);
                    var theta = baseStartPt.GetAngleTo(baseEndPt);

                    var theta1 = theta - alp2;
                    var theta2 = alp2 - alp1;
                    var theta3 = theta - alp1;
                    var theta4 = alp2 - alp3;
                    var theta5 = theta - alp3;

                    var f1 = Math.Sin(theta1) * Math.Sin(theta2) / Math.Sin(theta3);
                    var f2 = Math.Sin(theta1) * Math.Sin(theta4) / Math.Sin(theta5);
                    var f = f1 / f2;

                    var l1 = l / (1 + Math.Sqrt(f));
                    var l2 = l - l1;

                    var l3 = l1 * Math.Sin(theta1) / Math.Sin(theta3);
                    var l4 = l2 * Math.Sin(theta1) / Math.Sin(theta5);

                    var pt2 = p2.Polar(alp1, l3);
                    var pt3 = p3.Polar(alp3, -l4);

                    pline.UpgradeOpen();
                    pline.SetPointAt((int)par, pt2.GetPoint2d());
                    pline.SetPointAt((int)pos1, pt3.GetPoint2d());
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
    }
}
