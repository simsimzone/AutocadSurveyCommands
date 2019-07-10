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
        [CommandMethod("XXSRIGHT")]
        public void PLineStretchRightAngle()
        {
            Document doc = GetDocument();
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    Polyline pline = SelectPolyline(ed, trans,
                        "\nSelect a polyline: ", "\n>>>Select a polyline: ", true, out Point3d pickPt);
                    if (pline == null)
                        return;
                    bool closed = pline.Closed;
                    double param = pline.GetParameterAtPoint(pickPt);
                    int endParam = (int)pline.EndParam - (closed? 1 : 0);

                    int mIndex, fIndex, eIndex;
                    Point2d fPoint, mPoint, ePoint;
                    if (param - Math.Truncate(param) < 0.5)
                    {
                        mIndex = (int)param;
                        if (mIndex == 0 && !closed)
                        {
                            ed.WriteMessage("\nYou have selected the first segment," +
                                "Can't be a right angle :(");
                            return;
                        }
                        fIndex = mIndex + 1;
                        eIndex = mIndex == 0 ? endParam : mIndex - 1;
                    }
                    else
                    {
                        fIndex = (int)param;
                        mIndex = fIndex == endParam ? 0 : fIndex + 1;
                        if (mIndex == endParam && !closed)
                        {
                            ed.WriteMessage("\nYou have selected the last segment," +
                                "Can't be a right angle :(");
                            return;
                        }
                        eIndex = mIndex == endParam ? 0 : mIndex + 1;
                    }

                    fPoint = pline.GetPoint2dAt(fIndex);
                    mPoint = pline.GetPoint2dAt(mIndex);
                    ePoint = pline.GetPoint2dAt(eIndex);

                    double mf = fPoint.GetDistanceTo(mPoint);
                    double emf = mPoint.GetVectorTo(ePoint)
                        .GetAngleTo(mPoint.GetVectorTo(fPoint));
                    
                    double r = mf * Math.Sin(emf - Math.PI / 2);
                    if (emf < Math.PI / 2 && r > ePoint.GetDistanceTo(mPoint))
                    {
                        ed.WriteMessage("\nNo enough space :(");
                        return;
                    }
                    var newPoint = mPoint.Polar(ePoint, -r);
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
