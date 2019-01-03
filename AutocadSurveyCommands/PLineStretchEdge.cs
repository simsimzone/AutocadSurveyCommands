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
        [CommandMethod("XXSE")]
        public void StretchPolylineEdge()
        {
            doc = GetDocument();
            db = doc.Database;
            ed = doc.Editor;

            double requiredArea = 0.0;
            try
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
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

                    PromptEntityOptions peo = new PromptEntityOptions("\nSelect a polyline: ")
                    {
                        AllowNone = false
                    };
                    peo.SetRejectMessage("\n>>>this is not a polyline, Select a polyline: ");
                    peo.AddAllowedClass(typeof(Polyline), true);

                    //ed.TurnForcedPickOn();
                    //ed.PointMonitor += Ed_PointMonitor;

                    PromptEntityResult per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK)
                        return;
                    var pline = trans.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                    if (!pline.Closed)
                    {
                        ed.WriteMessage("\nThe selected polyline is not closed");
                        return;
                    }
                    var area = pline.GetArea();
                    var pickPt = pline.GetClosestPointTo(per.PickedPoint, true);

                    int par = (int)pline.GetParameterAtPoint(pickPt);
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
                    double h;
                    if (Math.Abs(ang1 - ang2) < 0.00001)
                        h = dA / l1;
                    else
                        h = (-l1 + Math.Sqrt(l1 * l1 + 4 * dA * f)) / (2.0 * f);
                    var pt2 = p2.Polar(ang1, h / Math.Sin(dAng1));
                    var pt3 = p3.Polar(ang2, h / Math.Sin(dAng2));

                    pline.UpgradeOpen();
                    pline.SetPointAt(par, pt2);
                    pline.SetPointAt(pos1, pt3);
                    trans.Commit();
                }
            }
            catch(Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(ex.Message);
            }
        }
    }
}
