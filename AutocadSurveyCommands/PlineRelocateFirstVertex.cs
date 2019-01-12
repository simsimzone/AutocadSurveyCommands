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
        [CommandMethod("XXFV")]
        public void PlineRelocateFirstVertex()
        {
            Document doc = GetDocument();
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    PromptEntityOptions peo = new PromptEntityOptions(
                        "\nSelect a closed polyline: ")
                    {
                        AllowNone = false
                    };
                    peo.SetRejectMessage("\n>>>Select a closed polyline: ");
                    peo.AddAllowedClass(typeof(Polyline), true);
                    PromptEntityResult per;
                    Polyline pline;
                    while (true) {
                        per = ed.GetEntity(peo);
                        if (per.Status == PromptStatus.Cancel)
                            return;
                        pline = trans.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                        if (per.Status == PromptStatus.OK && pline != null && pline.Closed)
                            break;
                    }
                    ed.WriteMessage("\n Closed Polyline selected");

                    PromptPointOptions ppo = new PromptPointOptions("\nSelect a destination base point: ")
                    {
                        BasePoint = pline.StartPoint,
                        AllowNone = false,
                        UseBasePoint = true
                    };

                    //var vertices = GetPolylinePoints(pline, 0);

                    PromptPointResult ppr;
                    while (true)
                    {
                        ppr = ed.GetPoint(ppo);
                        if (ppr.Status == PromptStatus.Cancel)
                            return;
                        if (ppr.Status == PromptStatus.OK)
                        {
                            break;
                        }
                    }
                    ed.WriteMessage("\nPoint selected...");

                    Point3d selectedPoint = pline.GetClosestPointTo(ppr.Value, true);
                    double selParam = pline.GetParameterAtPoint(selectedPoint);
                    int endParam = (int)pline.EndParam - 1;
                    int preParam = (int)selParam;
                    int posParam = preParam == endParam ? 0 : preParam + 1;
                    int param = (selParam - preParam) < 0.5 ? preParam : posParam;
                    var startPoints = GetPolylinePoints(pline, 0, param);
                    var endPoints = GetPolylinePoints(pline, param);
                    endPoints.AddRange(startPoints);

                    pline.UpgradeOpen();
                    SetPolylinePoints(pline, endPoints);
                    trans.Commit();
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(ex.Message);
            }
        }

        //private List<Point2d> GetPolylinePoints(Polyline pline)
        //{
        //    if (pline == null)
        //        return null;
        //    List<Point2d> vertices = new List<Point2d>();
        //    for (int i = 0, last = pline.NumberOfVertices; i < last; i++)
        //    {
        //        vertices.Add(pline.GetPoint2dAt(i));
        //    }
        //    return vertices;
        //}

        private List<Point2d> GetPolylinePoints(Polyline pline, int start, int len = -1)
        {
            if (pline == null)
                return null;
            List<Point2d> vertices = new List<Point2d>();
            if (len == -1 || start + len > pline.NumberOfVertices)
            {
                len = pline.NumberOfVertices - start;
            }
            for (int i = start, end = start + len; i < end; i++)
            {
                vertices.Add(pline.GetPoint2dAt(i));
            }
            return vertices;
        }

        private void SetPolylinePoints(Polyline pline, List<Point2d> points)
        {
            int min = Math.Min(points.Count, pline.NumberOfVertices);
            for (int i = 0; i < min; i++)
            {
                pline.SetPointAt(i, points[i]);
            }
            if (pline.NumberOfVertices == points.Count)
                return;
            if (pline.NumberOfVertices > points.Count)
            {
                for (int i = pline.NumberOfVertices; i >= min; i--)
                {
                    pline.RemoveVertexAt(i);
                }
            }
        }
        
    }
}
