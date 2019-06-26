using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using GI = Autodesk.AutoCAD.GraphicsInterface;

[assembly: CommandClass(
  typeof(AutocadSurveyCommands.AutocadSurveyCommands)
)]

namespace AutocadSurveyCommands
{
    public partial class AutocadSurveyCommands
    {
        [CommandMethod("AAA", CommandFlags.Modal)]
        public void AdjustAreaCommand()
        {
                Document doc = GetDocument();
                Database db = doc.Database;
                Editor ed = doc.Editor;
                try
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    ed.TurnForcedPickOn();
                    ed.PointMonitor += Ed_PointMonitor;
                    
                    PromptEntityOptions peo = new PromptEntityOptions("\nSelect a polyline: ")
                    {
                        AllowNone = false
                    };
                    peo.SetRejectMessage("\n>>>this is not a polyline, Select a polyline: ");

                    PromptEntityResult per = ed.GetEntity(peo);
                    if (per.Status == PromptStatus.OK)
                    {
                        trans.Commit();
                    }
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(ex.Message);
            }
            finally
            {
                ed.PointMonitor -= Ed_PointMonitor;
                if (currentId != ObjectId.Null)
                {
                    EraseTransientGraphics();
                    currentId = ObjectId.Null;
                }
            }
        }

        private ObjectId currentId = ObjectId.Null;
        Point3d center = new Point3d();
        double radius = 0.001;
        double glyphHeight;
        
        private void Ed_PointMonitor(object sender, PointMonitorEventArgs e)
        {
            Document doc = GetDocument();
            Database db = doc.Database;
            Editor ed = doc.Editor;
            try
            {
                var fsPaths = e.Context.GetPickedEntities();
                var pickedPt = e.Context.ComputedPoint;
                Point2d pixels = e.Context.DrawContext.Viewport.GetNumPixelsInUnitSquare(e.Context.RawPoint);
                int glyphSize = CustomObjectSnapMode.GlyphSize;
                glyphHeight = glyphSize / pixels.Y * 1.0;

                radius = glyphHeight / 2.0;
                center = e.Context.RawPoint + new Vector3d(3 * radius, 3 * radius, 0);

                // nothing under the mouse cursor.
                if (fsPaths == null || fsPaths.Length == 0)
                {
                    if (currentId != ObjectId.Null)
                    {
                        EraseTransientGraphics();
                        currentId = ObjectId.Null;
                    }
                    return;
                }
                var oIds = fsPaths[0].GetObjectIds();
                var id = oIds[oIds.GetUpperBound(0)];

                // hovering over the same object.
                if (currentId == id)
                {
                    UpdateTransientGraphics();
                }
                else
                {
                    using (Transaction trans = db.TransactionManager.StartTransaction())
                    {
                        
                        Polyline pline = trans.GetObject(id, OpenMode.ForRead) as Polyline;
                        EraseTransientGraphics();
                        
                        if (pline == null)
                        {
                            strCurrentShape = "X";
                            AddTransientGraphics("X");
                        }
                        else
                        {
                            strCurrentShape = "V";
                            AddTransientGraphics("V");
                        }
                        currentId = id;
                    }
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

        string strCurrentShape = "V";
        private void AddTransientGraphics(string shp)
        {
            CreateShape(center, radius, strCurrentShape);
            IntegerCollection col = new IntegerCollection();
            foreach (Entity ent in shape)
            {
                GI.TransientManager.CurrentTransientManager.AddTransient(
                ent, GI.TransientDrawingMode.DirectShortTerm, 128, col);
            }
        }
        
        private void UpdateTransientGraphics()
        {
            ModifyShape(center, radius, strCurrentShape);
            IntegerCollection col = new IntegerCollection();
            foreach (Entity ent in shape)
            {
                GI.TransientManager.CurrentTransientManager.UpdateTransient(
                ent, col);
            }
        }

        private void EraseTransientGraphics()
        {
            IntegerCollection col = new IntegerCollection();
            foreach (Entity entity in shape)
            {
                GI.TransientManager.CurrentTransientManager.EraseTransient(
                    entity, col);
                //entity.Dispose();
            }
            shape.Clear();
            //shape.Dispose();
        }

        DBObjectCollection shape = new DBObjectCollection();
        

        void CreateShape(Point3d center, double radius, string curShape)
        {
            if (curShape == "X")
            {
                Line line1 = new Line(
                    center + new Vector3d(-radius, -radius, 0),
                    center + new Vector3d(+radius, +radius, 0))
                {
                    ColorIndex = 1
                };
                Line line2 = new Line(
                    center + new Vector3d(-radius, +radius, 0),
                    center + new Vector3d(+radius, -radius, 0))
                {
                    ColorIndex = 1
                };
                shape = new DBObjectCollection
                {
                    line1,
                    line2
                };
            }
            else if (curShape == "V")
            {
                Line line3 = new Line(
                    center + new Vector3d(-radius, -radius / 2.0, 0),
                    center + new Vector3d(-radius / 2.0, -radius, 0)
                    )
                {
                    ColorIndex = 3
                };
                Line line4 = new Line(
                    center + new Vector3d(-radius / 2.0, -radius, 0),
                    center + new Vector3d(radius, radius, 0)
                    )
                {
                    ColorIndex = 3
                };
                shape = new DBObjectCollection
                {
                    line3,
                    line4
                };
            }
        }

        void ModifyShape(Point3d center, double radius, string curShape)
        {
            if (curShape == "X")
            {
                Line l1 = shape[0] as Line;
                l1.StartPoint = center + new Vector3d(-radius, -radius, 0);
                l1.EndPoint = center + new Vector3d(+radius, +radius, 0);
                Line l2 = shape[1] as Line;
                l2.StartPoint = center + new Vector3d(-radius, +radius, 0);
                l2.EndPoint = center + new Vector3d(+radius, -radius, 0);
            }
            else if (curShape == "V")
            {
                Line l1 = shape[0] as Line;
                l1.StartPoint = center + new Vector3d(-radius, -radius / 2.0, 0);
                l1.EndPoint = center + new Vector3d(-radius / 2.0, -radius, 0);
                Line l2 = shape[1] as Line;
                l2.StartPoint = center + new Vector3d(-radius / 2.0, -radius, 0);
                l2.EndPoint = center + new Vector3d(radius, radius, 0);
            }
        }


        //private void Source(object sender, PointMonitorEventArgs e)
        //{
        //    //string info = "";
        //    var fsPaths = e.Context.GetPickedEntities();
        //    var pickedPt = e.Context.ComputedPoint;
        //    using (Transaction trans = db.TransactionManager.StartTransaction())
        //    {
        //        //Nothing to hover..
        //        if (fsPaths == null || fsPaths.Length == 0)
        //        {
        //            //check if previous hovering
        //            if (poly != null && hoveredElement != null)
        //            {
        //                poly.Unhighlight(hoveredElement.Value, true);
        //                poly = null;
        //                hoveredElement = null;
        //            }
        //            return;
        //        }
        //        var oIds = fsPaths[0].GetObjectIds();
        //        var id = oIds[oIds.GetUpperBound(0)];

        //        if (poly != null && !poly.ObjectId.Equals(id) && hoveredElement != null)
        //        {
        //            poly.Unhighlight(hoveredElement.Value, true);
        //            poly = null;
        //            hoveredElement = null;
        //        }

        //        poly = trans.GetObject(id, OpenMode.ForRead) as Polyline;
        //        if (poly == null)
        //            return;

        //        var pp = poly.GetClosestPointTo(pickedPt, true);
        //        var param = (int)poly.GetParameterAtPoint(pp);
        //        if (hoveredElement != null &&
        //            hoveredElement.Value.SubentId.IndexPtr.ToInt32() != param)
        //        {
        //            poly.Unhighlight(hoveredElement.Value, false);
        //        }

        //        hoveredElement = new FullSubentityPath(
        //            new ObjectId[] { id },
        //            new SubentityId(SubentityType.Edge, new IntPtr((long)param + 1)));
        //        if (hoveredElement == null)
        //            return;
        //        poly.Highlight(hoveredElement.Value, false);
        //        //info += hoveredElement.Value.SubentId.IndexPtr.ToInt32().ToString() + "\n";
        //    }
        //    //if (info != "")
        //    //e.AppendToolTipText(info);
        //    Point2d pixels = e.Context.DrawContext.Viewport.GetNumPixelsInUnitSquare(e.Context.RawPoint);
        //    AddTransientGraphics(e.Context.RawPoint, 10.0 / pixels.X);
        //}
    }
}
