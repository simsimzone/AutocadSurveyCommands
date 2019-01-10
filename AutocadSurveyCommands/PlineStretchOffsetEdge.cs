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

namespace AutocadSurveyCommands
{
    public partial class AutocadSurveyCommands
    {

        [CommandMethod("XXSOE")]
        public void ZZ_StretchOffsetEdge()
        {
            Document doc = GetDocument();
            Database db = doc.Database;
            Editor ed = doc.Editor;

            double requiredOffset = 0;
            try
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    PromptDistanceOptions pdo = new PromptDistanceOptions(
                        "\nSpecify the required offset: ")
                    {
                        AllowNegative = true,
                        AllowNone = false,
                        AllowZero = true,
                        DefaultValue = defaultOffset,
                        UseDefaultValue = defaultOffset == 0 ? false : true
                    };
                    PromptDoubleResult pdr = ed.GetDistance(pdo);
                    if (pdr.Status != PromptStatus.OK)
                        return;

                    requiredOffset = defaultOffset = pdr.Value;

                    PromptEntityOptions peo = new PromptEntityOptions("\nSelect a polyline edge: ")
                    {
                        AllowNone = false
                    };
                    peo.SetRejectMessage("\n>>>this is not a polyline, Select a polyline edge: ");
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

                    double ang1 = p1.GetVectorTo(p2).Angle;
                    double ang2 = p4.GetVectorTo(p3).Angle;
                    double ang = p2.GetVectorTo(p3).Angle;
                    double dAng1 = (area > 0) ? ang - ang1 : ang1 - ang;
                    double dAng2 = (area > 0) ? ang - ang2 : ang2 - ang;

                    var pt2 = p2.Polar(ang1, requiredOffset / Math.Sin(dAng1));
                    var pt3 = p3.Polar(ang2, requiredOffset / Math.Sin(dAng2));

                    pline.UpgradeOpen();
                    pline.SetPointAt(par, pt2);
                    pline.SetPointAt(pos1, pt3);
                    trans.Commit();
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(ex.Message);
            }
        }
    }
}
