/*
----------------------------------------------------------------------------
  Author:  Sami Abdelgadir Mohammed, copyright © 2004-2019
----------------------------------------------------------------------------
  Email:   simsimzone @yahoo.co.uk
----------------------------------------------------------------------------
  Version 3.0     -      05-JUNE-2019
----------------------------------------------------------------------------
*/
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
        [CommandMethod("XXMIDPOINT")]
        public void MidPoint()
        {
            Document doc = GetDocument();
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    PromptSelectionOptions pso = new PromptSelectionOptions();
                    TypedValue[] tv = new TypedValue[] { new TypedValue(0, "POINT") };
                    SelectionFilter filter = new SelectionFilter(tv);

                    PromptSelectionResult psr = ed.GetSelection(filter);

                    if (psr.Status != PromptStatus.OK)
                        return;

                    var res = psr.Value.GetObjectIds();
                    double x = 0, y = 0;
                    int n = 0;
                    foreach (ObjectId objectId in res)
                    {
                        var pt = trans.GetObject(objectId, OpenMode.ForRead) as DBPoint;
                        if (pt != null)
                        {
                            n++;
                            x += pt.Position.X;
                            y += pt.Position.Y;
                        }
                    }
                    if (n == 0)
                        return;

                    var avgPt = new Point3d(x / n, y / n, 0);

                    BlockTable bt = trans.GetObject(db.BlockTableId
                        , OpenMode.ForRead) as BlockTable;

                    BlockTableRecord btr = trans.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite) as BlockTableRecord;

                    DBPoint po = new DBPoint(avgPt);
                    btr.AppendEntity(po);
                    trans.AddNewlyCreatedDBObject(po, true);

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
