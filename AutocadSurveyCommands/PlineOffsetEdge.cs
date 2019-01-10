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
        [CommandMethod("XXOE")]
        public void ZZ_OffsetEdge()
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



                    PromptEntityResult per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK)
                        return;
                    var pline = trans.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                    var pickPt = pline.GetClosestPointTo(per.PickedPoint, true);

                    PromptPointOptions ppo = new PromptPointOptions("\nSelect a side: ")
                    {
                        AllowNone = false
                    };
                    PromptPointResult ppr = ed.GetPoint(ppo);
                    if (ppr.Status != PromptStatus.OK)
                        return;
                    var p3 = ppr.Value.GetPoint2d();

                    int par = (int)pline.GetParameterAtPoint(pickPt);
                    int pos1 = par + 1 == (int)pline.EndParam &&
                       pline.Closed ? 0 : par + 1;

                    // get the the surrounding points
                    var p1 = pline.GetPointAtParameter(par).GetPoint2d();
                    var p2 = pline.GetPointAtParameter(pos1).GetPoint2d();

                    double ang = p1.GetVectorTo(p2).Angle;
                    bool cw = Clockwise(p1, p2, p3);
                    double ang1 = cw ? ang - Math.PI * 0.5 : ang + Math.PI * 0.5;
                    ed.WriteMessage("\nAng = " + ang);
                    ed.WriteMessage("\nAng1 = " + ang1);
                    var pt1 = p1.Polar(ang1, requiredOffset).GetPoint3d();
                    var pt2 = p2.Polar(ang1, requiredOffset).GetPoint3d();


                    BlockTable blockTable = trans.GetObject(db.BlockTableId, OpenMode.ForRead)
                        as BlockTable;
                    BlockTableRecord record = trans.GetObject(blockTable[BlockTableRecord.ModelSpace]
                        , OpenMode.ForWrite) as BlockTableRecord;
                    Line line = new Line(pt1, pt2);
                    line.SetDatabaseDefaults();
                    record.AppendEntity(line);
                    trans.AddNewlyCreatedDBObject(line, true);
                    trans.Commit();
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(ex.Message);
            }
        }

        private bool Clockwise(Point2d p1, Point2d p2, Point2d p3)
        {
            return ((p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X)) < 1e-9;
        }
    }
}
