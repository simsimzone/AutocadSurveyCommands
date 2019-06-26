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
        [CommandMethod("XXPOINTS")]
        public void PLinePoints()
        {
            Document doc = GetDocument();
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {

                    PromptEntityOptions peo = new PromptEntityOptions(
                        "\nSelect a polyline: ")
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

                    BlockTable bt = trans.GetObject(db.BlockTableId
                        , OpenMode.ForRead) as BlockTable;

                    BlockTableRecord btr = trans.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite) as BlockTableRecord;

                    for (int i = 0, len = pline.NumberOfVertices; i < len; i++)
                    {
                        DBPoint po = new DBPoint(pline.GetPoint3dAt(i));
                        btr.AppendEntity(po);
                        trans.AddNewlyCreatedDBObject(po, true);
                    }
                    db.Pdsize = -2;
                    db.Pdmode = 35;

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
