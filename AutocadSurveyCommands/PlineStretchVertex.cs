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
        [CommandMethod("XXSV")]
        public void StretchPolylineVertex()
        {
            Document doc = GetDocument();
            Database db = doc.Database;
            Editor ed = doc.Editor;

            double requiredArea = 0;
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

                    var l1 = movablePoint.GetDistanceTo(fixedPoint);
                    var dA = requiredArea - Math.Abs(area);
                    var ang1 = fixedPoint.GetVectorTo(movablePoint).Angle;
                    var ang2 = extPoint.GetVectorTo(movablePoint).Angle;

                    var ang = ang1 - ang2;
                    var dl = 2 * dA / (l1 * Math.Abs(Math.Sin(ang)));
                    var finaltPoint = movablePoint.Polar(ang2, dl);

                    pline.UpgradeOpen();
                    pline.SetPointAt(mIndex, finaltPoint);
                    trans.Commit();
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(ex.Message);
            }
        }

        //private void Ed_PointMonitor(object sender, PointMonitorEventArgs e)
        //{

        //}
    }
}
