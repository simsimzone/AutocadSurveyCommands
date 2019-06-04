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
        [CommandMethod("XXCOORDS")]
        public void PlineCoords()
        {
            ///*
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
                    while (true)
                    {
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
                        AllowNone = false,
                    };

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
                    Point3d insertionPoint = ppr.Value;

                    BlockTable bt = trans.GetObject(db.BlockTableId
                        , OpenMode.ForRead) as BlockTable;

                    BlockTableRecord btr = trans.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite) as BlockTableRecord;

                    // make sure pline is in clockwise direction
                    double area = pline.GetArea();
                    if (area < 0.0)
                    {
                        area = -area;
                        // I 'll do it later
                        //pline.ReverseCurve();
                    }

                    // get pline points
                    var list = pline.GetPolylinePoints(0);

                    var count = list.Count;

                    Table table = new Table();
                    table.SetDatabaseDefaults();
                    table.SetSize(count + 2, 4);

                    table.Cells.Alignment = CellAlignment.MiddleCenter;
                    table.Rows[0].Alignment = CellAlignment.MiddleLeft;
                    table.Cells[0, 2].Alignment = CellAlignment.MiddleCenter;

                    table.Cells.TextHeight = 3.0;

                    int i, j;
                    string from, to, northing, easting, ds;

                    table.Columns[0].Width = 17.0;
                    table.Columns[1].Width = 36.0;
                    table.Columns[2].Width = 35.0;
                    table.Columns[3].Width = 19.0;


                    //table.Rows[0].Height = 5.0;
                    //table.Rows[1].Height = 5.0;

                    CellRange range1 = CellRange.Create(table, 0, 0, 0, 1);
                    CellRange range2 = CellRange.Create(table, 0, 2, 0, 3);

                    table.MergeCells(range2);
                    table.MergeCells(range1);

                    table.Cells[0, 0].TextString = "Coordinates:";
                    //table.Cells[0, 2].TextString = $"Area: {area:F0} m\xB2";
                    
                    string textArea = $"{area:F0}";
                    string text = "{\\fMonospac821 BT|b1|;\\L\\C1;" + textArea + "}";
                    table.Cells[0, 2].TextString = "Area: " + text + " m\xB2";
                    //"Coordinates , Area =  {0:0.00} SQ.M", area);
                    //table.VerticalCellMargin=
                    
                    table.Rows[0].Height = 6.0;


                    table.Cells[1, 0].TextString = "LINE";
                    table.Cells[1, 1].TextString = "NORTHING";
                    table.Cells[1, 2].TextString = "EASTING";
                    table.Cells[1, 3].TextString = "DIST(m)";

                    string spaces = "";

                    for (i = 0, j = 1; i < count; i++, j++)
                    {
                        var p1 = pline.GetPoint2dAt(i);
                        var p2 = pline.GetPoint2dAt(j);
                        from = (i + 1).ToString();
                        to = (j + 1).ToString();
                        northing = p1.Y.ToString("0.00");
                        easting = p1.X.ToString("0.00");
                        ds = p1.GetDistanceTo(p2).ToString("0.00");

                        spaces = "  ";
                        if (i < 9)
                            spaces += " ";
                        if (j < 9)
                            spaces += " ";
                        //table.Rows[i + 2].Height = 5.0;
                        
                        table.Cells[i + 2, 0].TextString = from + spaces + to;
                        table.Cells[i + 2, 1].TextString = northing;
                        table.Cells[i + 2, 2].TextString = easting;
                        table.Cells[i + 2, 3].TextString = ds;


                        // check if we are out of bounds, then make j -1 so after increment
                        // it will reset to 0 (the first point).
                        if (j == count - 1)
                            j = -1;


                        MText mt = new MText();
                        mt.SetDatabaseDefaults();
                        mt.Contents = from;
                        mt.TextHeight = 2.0;
                        
                        // location logic to be recalculated
                        mt.Location = p1.GetPoint3d();
                        btr.AppendEntity(mt);
                        trans.AddNewlyCreatedDBObject(mt, true);
                        //trans.Commit();
                    }
                    table.Rows[0].Height = 7.0;
                    table.Position = insertionPoint;
                    table.GenerateLayout();

                    

                    btr.AppendEntity(table);
                    trans.AddNewlyCreatedDBObject(table, true);
                    trans.Commit();


                    ed.WriteMessage($"\n{table.Rows[0].Height}");
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage(ex.Message);
            }
            //*/

            
        }

        public void CreateTextStyle()
        {
            // hi {fMonospac821 BT|b1|i0|c0|p49;\C1;all\C256; }m
            // hi {fMonospac821 BT|b1|i0|c0|p49;\L\C1;all\l\C256; }another text%%d

        }
    }
}




/*
            Document doc =

              Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;

            Editor ed = doc.Editor;


            PromptPointResult pr =

              ed.GetPoint("\nEnter table insertion point: ");

            if (pr.Status == PromptStatus.OK)

            {

                Table tb = new Table();

                tb.TableStyle = db.Tablestyle;

                tb.NumRows = 5;

                tb.NumColumns = 3;

                tb.SetRowHeight(3);

                tb.SetColumnWidth(15);

                tb.Position = pr.Value;


                // Create a 2-dimensional array

                // of our table contents

                string[,] str = new string[5, 3];

                str[0, 0] = "Part No.";

                str[0, 1] = "Name ";

                str[0, 2] = "Material ";

                str[1, 0] = "1876-1";

                str[1, 1] = "Flange";

                str[1, 2] = "Perspex";

                str[2, 0] = "0985-4";

                str[2, 1] = "Bolt";

                str[2, 2] = "Steel";

                str[3, 0] = "3476-K";

                str[3, 1] = "Tile";

                str[3, 2] = "Ceramic";

                str[4, 0] = "8734-3";

                str[4, 1] = "Kean";

                str[4, 2] = "Mostly water";


                // Use a nested loop to add and format each cell

                for (int i = 0; i < 5; i++)

                {

                    for (int j = 0; j < 3; j++)

                    {

                        tb.SetTextHeight(i, j, 1);

                        tb.SetTextString(i, j, str[i, j]);

                        tb.SetAlignment(i, j, CellAlignment.MiddleCenter);

                    }

                }

                tb.GenerateLayout();


                Transaction tr =

                  doc.TransactionManager.StartTransaction();

                using (tr)

                {

                    BlockTable bt =

                      (BlockTable)tr.GetObject(

                        doc.Database.BlockTableId,

                        OpenMode.ForRead

                      );

                    BlockTableRecord btr =

                      (BlockTableRecord)tr.GetObject(

                        bt[BlockTableRecord.ModelSpace],

                        OpenMode.ForWrite

                      );

                    btr.AppendEntity(tb);

                    tr.AddNewlyCreatedDBObject(tb, true);

                    tr.Commit();

                }

            }
*/
