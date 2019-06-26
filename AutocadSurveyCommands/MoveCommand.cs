using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using System;


[assembly: CommandClass(
  typeof(PolylineCommands.PolylineCommands)
)]

namespace PolylineCommands
{

    public partial class PolylineCommands
    {
        [CommandMethod("MYMOVE", CommandFlags.UsePickSet)]
        public static void CustomMoveCmd()
        {
            Document doc =
              Autodesk.AutoCAD.ApplicationServices.
                Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Start by getting the objects to move

            // (will use the pickfirst set, if defined)

            PromptSelectionResult psr = ed.GetSelection();
            if (psr.Status != PromptStatus.OK || psr.Value.Count == 0)
                return;
            // Create a collection of the selected objects' IDs

            ObjectIdCollection ids =
              new ObjectIdCollection(psr.Value.GetObjectIds());

            // Ask the user to select a base point for the move

            PromptPointResult ppr =
              ed.GetPoint("\nSpecify base point: ");
            if (ppr.Status != PromptStatus.OK)
                return;

            Point3d basePt = ppr.Value;
            Point3d curPt = basePt;

            // A local delegate for our event handler so
            // we can remove it at the end
            PointMonitorEventHandler handler = null;
            // Our transaction

            Transaction tr =
              doc.Database.TransactionManager.StartTransaction();
            using (tr)
            {
                // Create our transient drawables, with associated
                // graphics, from the selected objects

                List<Drawable> drawables = CreateTransGraphics(tr, ids);
                try
                {
                    // Add our point monitor
                    // (as a delegate we have access to basePt and curPt,
                    //  which avoids having to access global/member state)
                    handler =
                      delegate (object sender, PointMonitorEventArgs e)
                      {
                          // Get the point, with "ortho" applied, if needed

                          Point3d pt = e.Context.RawPoint;
                          if (IsOrthModeOn())
                              pt = GetOrthoPoint(basePt, pt);
                          // Update our graphics and the current point
                          UpdateTransGraphics(drawables, curPt, pt);
                          curPt = pt;
                      };
                    ed.PointMonitor += handler;
                    // Ask for the destination, during which the point
                    // monitor will be updating the transient graphics
                    PromptPointOptions opt =
                      new PromptPointOptions("\nSpecify second point: ");
                    opt.UseBasePoint = true;
                    opt.BasePoint = basePt;
                    ppr = ed.GetPoint(opt);
                    // If the point was selected successfully...
                    if (ppr.Status == PromptStatus.OK)
                    {
                        // ... move the entities to their destination
                        MoveEntities(
                          tr, basePt,
                          IsOrthModeOn() ?
                            GetOrthoPoint(basePt, ppr.Value) :
                            ppr.Value,
                          ids
                        );
                        // And inform the user
                        ed.WriteMessage(
                          "\n{0} object{1} moved", ids.Count,
                          ids.Count == 1 ? "" : "s"
                        );
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    ed.WriteMessage("\nException: {0}", ex.Message);
                }
                finally
                {
                    // Clear any transient graphics
                    ClearTransGraphics(drawables);
                    // Remove the event handler
                    if (handler != null)
                        ed.PointMonitor -= handler;
                    tr.Commit();
                    tr.Dispose();
                }
            }
        }

        private static bool IsOrthModeOn()
        {
            // Check the value of the ORTHOMODE sysvar
            object orth =
              Autodesk.AutoCAD.ApplicationServices.Application.
              GetSystemVariable("ORTHOMODE");
            return Convert.ToInt32(orth) > 0;
        }

        private static Point3d GetOrthoPoint(
          Point3d basePt, Point3d pt
        )
        {
            // Apply a crude orthographic mode
            double x = pt.X;
            double y = pt.Y;
            Vector3d vec = basePt.GetVectorTo(pt);
            if (Math.Abs(vec.X) >= Math.Abs(vec.Y))
                y = basePt.Y;
            else
                x = basePt.X;
            return new Point3d(x, y, 0.0);
        }

        private static void MoveEntities(
          Transaction tr, Point3d basePt, Point3d moveTo,
          ObjectIdCollection ids
        )
        {
            // Transform a set of entities to a new location
            Matrix3d mat =
              Matrix3d.Displacement(basePt.GetVectorTo(moveTo));
            foreach (ObjectId id in ids)
            {
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                ent.TransformBy(mat);
            }
        }

        private static List<Drawable> CreateTransGraphics(
          Transaction tr, ObjectIdCollection ids
        )
        {
            // Create our list of drawables to return
            List<Drawable> drawables = new List<Drawable>();
            foreach (ObjectId id in ids)
            {
                // Read each entity
                Entity ent = (Entity)tr.GetObject(id, OpenMode.ForRead);
                // Clone it, make it red & add the clone to the list
                Entity drawable = ent.Clone() as Entity;
                drawable.ColorIndex = 1;
                drawables.Add(drawable);
            }

            // Draw each one initially

            foreach (Drawable d in drawables)
            {
                TransientManager.CurrentTransientManager.AddTransient(
                  d, TransientDrawingMode.DirectShortTerm,
                  128, new IntegerCollection()
                );
            }
            return drawables;
        }

        private static void UpdateTransGraphics(
          List<Drawable> drawables, Point3d curPt, Point3d moveToPt
        )
        {
            // Displace each of our drawables
            Matrix3d mat =
              Matrix3d.Displacement(curPt.GetVectorTo(moveToPt));
            // Update their graphics
            foreach (Drawable d in drawables)
            {
                Entity e = d as Entity;
                e.TransformBy(mat);
                TransientManager.CurrentTransientManager.UpdateTransient(
                  d, new IntegerCollection()
                );
            }
        }

        private static void ClearTransGraphics(
          List<Drawable> drawables
        )
        {
            // Clear the transient graphics for our drawables
            TransientManager.CurrentTransientManager.EraseTransients(
              TransientDrawingMode.DirectShortTerm,
              128, new IntegerCollection()
            );
            // Dispose of them and clear the list
            foreach (Drawable d in drawables)
            {
                d.Dispose();
            }
            drawables.Clear();
        }
    }
}