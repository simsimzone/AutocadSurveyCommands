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
        [CommandMethod("XXPOINTINSIDE")]
        public void PLinePointInside()
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
                    var ptRes = GetPoint3D(ed, trans, "\nPick a point: ", "\nPick a point: ");
                    if (ptRes == null)
                        return;
                    var pt = ptRes.Value.GetPoint2d();

                    double sum = AngleSum(pt, pline);
                    if (Math.Abs(sum) < 0.0001)
                    {
                        ed.WriteMessage("\nOutside...");
                    }
                    else
                    {
                        ed.WriteMessage("\nInside...");
                    }
                    
                    trans.Commit();
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(ex.Message + ex.StackTrace);
            }

            double AngleSum(Point2d pt, Polyline pline)
            {
                Point2d p1, p2;
                double sum = 0.0, ang;
                int i, len;
                for (i = 0, len = pline.NumberOfVertices - 1; i < len; i++)
                {
                    p1 = pline.GetPoint2dAt(i);
                    p2 = pline.GetPoint2dAt(i + 1);
                    ang = GetAngle(pt, p1, p2);
                    sum += ang;
                }
                ang = GetAngle(pt, pline.GetPoint2dAt(i), pline.GetPoint2dAt(0));
                sum += ang;
                return sum;
            }

            double GetAngle(Point2d pt, Point2d p1, Point2d p2)
            {
                double ang = pt.AngleTo(p2) - pt.AngleTo(p1);
                if (ang < 0)
                {
                    ang += Math.PI * 2.0;
                }
                if (ang > Math.PI)
                {
                    ang -= Math.PI * 2.0;
                }
                return ang;
            }

        }
    }
}
