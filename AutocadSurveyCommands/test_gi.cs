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

        [CommandMethod("gi")]
        public void GetIntersections()
        {
            Database db = HostApplicationServices.WorkingDatabase;

            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

            Editor ed = doc.Editor;

            Transaction tr = db.TransactionManager.StartTransaction();

            using (tr)
            {
                try
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    PromptEntityOptions peo = new PromptEntityOptions("\nSelect a single polyline  >>");

                    peo.SetRejectMessage("\nSelected object might be of type polyline only >>");

                    peo.AddAllowedClass(typeof(Polyline), false);

                    PromptEntityResult res;

                    res = ed.GetEntity(peo);

                    if (res.Status != PromptStatus.OK)

                        return;

                    DBObject ent = (DBObject)tr.GetObject(res.ObjectId, OpenMode.ForRead);

                    if (ent == null) return;

                    //Polyline poly = (Polyline)ent as Polyline;
                    Curve curv = ent as Curve;

                    DBObjectCollection pcurves = new DBObjectCollection();

                    curv.Explode(pcurves);
                    TypedValue[] values = new TypedValue[]
                     {
                        new TypedValue(0, "lwpolyline")
                        //might be added layer name to select curve:
                        //, new TypedValue(8, "mylayer")
                     };
                    SelectionFilter filter = new SelectionFilter(values);

                    Point3dCollection fence = new Point3dCollection();

                    double leng = curv.GetDistanceAtParameter(curv.EndParam) - curv.GetDistanceAtParameter(curv.StartParam);
                    // number of divisions along polyline to create fence selection
                    double step = leng / 256;// set number of steps to your suit

                    int num = Convert.ToInt32(leng / step);

                    int i = 0;

                    for (i = 0; i < num; i++)
                    {
                        Point3d pp = curv.GetPointAtDist(step * i);

                        fence.Add(curv.GetClosestPointTo(pp, false));
                    }

                    PromptSelectionResult selres = ed.SelectFence(fence, filter);

                    if (selres.Status != PromptStatus.OK) return;
                    Point3dCollection intpts = new Point3dCollection();

                    DBObjectCollection qcurves = new DBObjectCollection();

                    foreach (SelectedObject selobj in selres.Value)
                    {
                        DBObject obj = tr.GetObject(selobj.ObjectId, OpenMode.ForRead, false) as DBObject;
                        if (selobj.ObjectId != curv.ObjectId)
                        {
                            DBObjectCollection icurves = new DBObjectCollection();
                            Curve icurv = obj as Curve;
                            icurv.Explode(icurves);
                            foreach (DBObject dbo in icurves)
                            {
                                if (!qcurves.Contains(dbo))
                                    qcurves.Add(dbo);
                            }
                        }

                    }
                    ed.WriteMessage("\n{0}", qcurves.Count);



                    int j = 0;
                    Point3dCollection polypts = new Point3dCollection();

                    for (i = 0; i < pcurves.Count; ++i)
                    {
                        for (j = 0; j < qcurves.Count; ++j)
                        {
                            Curve curve1 = pcurves[i] as Curve;

                            Curve curve2 = qcurves[j] as Curve;

                            Point3dCollection pts = new Point3dCollection();

                            curve1.IntersectWith(curve2, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);

                            foreach (Point3d pt in pts)
                            {
                                if (!polypts.Contains(pt))
                                    polypts.Add(pt);
                            }
                        }
                    }

                    Autodesk.AutoCAD.ApplicationServices.Core.Application.SetSystemVariable("osmode", 0);// optional
                    // for debug only
                    Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowAlertDialog(string.Format("\nNumber of Intersections: {0}", polypts.Count));
                    // test for visulization only
                    foreach (Point3d inspt in polypts)
                    {
                        Circle circ = new Circle(inspt, Vector3d.ZAxis, 10 * db.Dimtxt)
                        {
                            ColorIndex = 1
                        };
                        btr.AppendEntity(circ);
                        tr.AddNewlyCreatedDBObject(circ, true);

                    }
                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage("\n{0}\n{1}", ex.Message, ex.StackTrace);
                }
            }

        }
    }
}
