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
        [CommandMethod("XXVRIGHT")]
        public void PLineVertexRightAngle()
        {
            Document doc = GetDocument();
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {

                    PromptEntityOptions peo = new PromptEntityOptions(
                        "\nSelect a polyline\u299C: ")
                    {
                        AllowNone = false
                    };
                    peo.SetRejectMessage("\n>>>Select a polyline: ");
                    peo.AddAllowedClass(typeof(Polyline), true);
                    PromptEntityResult per;
                    Polyline pline;
                    while (true)
                    {
                        per = ed.GetEntity(peo);
                        if (per.Status == PromptStatus.Cancel)
                            return;
                        pline = trans.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                        if (per.Status == PromptStatus.OK && pline != null)
                            break;
                    }

                    //pline = trans.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                    //if (!pline.Closed)
                    //{
                    //    ed.WriteMessage("\nThe selected polyline is not closed");
                    //    return;
                    //}
                    var pickedPt = pline.GetClosestPointTo(per.PickedPoint, true);
                    
                    double param = pline.GetParameterAtPoint(pickedPt);
                    int mIndex, fIndex;// we need extra var
                    int endParam = (int)pline.EndParam - 1;

                    Point2d fPoint, mPoint, f0Point, m0Point;
                    if (param - Math.Truncate(param) < 0.5)
                    {
                        fIndex = (int)param;
                        mIndex = (int)param == endParam ? 0 : (int)param + 1;
                        fPoint = pline.GetPoint2dAt(fIndex);
                        mPoint = pline.GetNextPoint2d(fPoint);
                        f0Point = pline.GetPreviousPoint2d(fPoint);
                        m0Point = pline.GetNextPoint2d(mPoint);
                    }
                    else
                    {
                        fIndex = (int)param == endParam ? 0 : (int)param + 1;
                        mIndex = (int)param == 0 ? endParam : (int)param;
                        fPoint = pline.GetPoint2dAt(fIndex);
                        mPoint = pline.Point2dBefore(fIndex);
                        f0Point = pline.Point2dAfter(fIndex);
                        m0Point = pline.Point2dBefore(fIndex - 1);
                    }

                    double mf = fPoint.GetDistanceTo(mPoint);
                    double f0fm = fPoint.GetVectorTo(mPoint)
                        .GetAngleTo(fPoint.GetVectorTo(f0Point));
                    double m0mf = mPoint.GetVectorTo(fPoint)
                        .GetAngleTo(mPoint.GetVectorTo(m0Point));
                    double r = mf * Math.Sin(Math.PI / 2 - f0fm)
                        / Math.Sin(f0fm + m0mf - Math.PI / 2);
                    var newPoint = mPoint.Polar(m0Point, -r);
                    pline.UpgradeOpen();
                    pline.SetPointAt(mIndex, newPoint);
                    trans.Commit();
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(ex.Message + ex.StackTrace);
            }
        }

        

    }
}
