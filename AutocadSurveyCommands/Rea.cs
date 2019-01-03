using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

namespace AutocadSurveyCommands
{
    public partial class AutocadSurveyCommands
    {

        [CommandMethod("Rea", CommandFlags.Session)]
        public static void testForUnionPlines()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            // get argument to choose boolean operation mode
            PromptKeywordOptions pko = new PromptKeywordOptions("\nChoose boolean operation mode " + "[Union/Subtract]: ", "Union Subtract");
            // The default depends on our current settings
            pko.Keywords.Default = "Union";
            PromptResult pkr = ed.GetKeywords(pko);
            if (pkr.Status != PromptStatus.OK) return;
            string choice = pkr.StringResult;

            bool doUnion = choice == "Union" ? true : false;
            List<Region> regLst = new List<Region>();
            List<Polyline> delPline = new List<Polyline>();
            using (DocumentLock doclock = doc.LockDocument())
            {
                //start a transaction
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {

                    TypedValue[] tvs = new TypedValue[3]
            {new TypedValue(0, "lwpolyline"),
                new TypedValue(-4, "&"),
                new TypedValue(70, 1)
            };
                    SelectionFilter filter = new SelectionFilter(tvs);
                    PromptSelectionOptions pso = new PromptSelectionOptions();
                    pso.MessageForRemoval = "\nSelect closed polylines only: ";
                    pso.MessageForAdding = "\nSelect closed polylines: ";
                    PromptSelectionResult result = ed.GetSelection(filter);
                    if (result.Status != PromptStatus.OK) return;

                    try
                    {
                        SelectionSet sset = result.Value;
                        ObjectId[] ids = sset.GetObjectIds();
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite, false);
                        Region objreg1 = new Region();

                        for (int n = 0; n < ids.Count(); n++)
                        {

                            DBObject obj = tr.GetObject(ids[n], OpenMode.ForRead) as DBObject;
                            Polyline pline1 = obj as Polyline;
                            if (pline1 == null) return;
                            // Add the polyline to the List to rerase them all at the end of execution
                            delPline.Add(pline1);
                            // Add the polyline to the array
                            DBObjectCollection objArray1 = new DBObjectCollection();
                            objArray1.Add(pline1);
                            // create the 1 st region
                            DBObjectCollection objRegions1 = new DBObjectCollection();
                            objRegions1 = Region.CreateFromCurves(objArray1);
                            objreg1 = objRegions1[0] as Region;
                            btr.AppendEntity(objreg1);

                            tr.AddNewlyCreatedDBObject(objreg1, true);

                            objreg1.ColorIndex = 1;//optional
                            // add the region to the List<Region> for the future work
                            regLst.Add(objreg1);
                        }
                        //ed.WriteMessage("\nCount regions:\t{0}\n", regLst.Count);//just for the debug

                        // sort regions by areas
                        Region[] items = regLst.ToArray();
                        Array.Sort(items, (Region x, Region y) => y.Area.CompareTo(x.Area));
                        // get the biggest region first
                        Region mainReg = items[0];
                        // ed.WriteMessage("\nMain region area:\t{0:f3}\n", items[0].Area);//just for the debug
                        if (!mainReg.IsWriteEnabled) mainReg.UpgradeOpen();
                        if (items.Length == 2)
                        {

                            if (!doUnion)
                            {
                                mainReg.BooleanOperation(BooleanOperationType.BoolSubtract, (Region)items[1]);
                            }
                            else
                            {
                                mainReg.BooleanOperation(BooleanOperationType.BoolUnite, (Region)items[1]);
                            }
                        }
                        else
                        {
                            // starting iteration from the second region
                            int i = 1;
                            do
                            {
                                Region reg1 = items[i]; Region reg2 = items[i + 1];

                                if ((reg1 == null) || (reg2 == null))
                                {
                                    break;
                                }

                                else
                                {
                                    // subtract region 1 from region 2
                                    if (reg1.Area > reg2.Area)
                                    {
                                        // subtract the smaller region from the larger one
                                        // 
                                        reg1.BooleanOperation(BooleanOperationType.BoolUnite, reg2);
                                        if (!doUnion)
                                        {
                                            mainReg.BooleanOperation(BooleanOperationType.BoolSubtract, reg1);
                                        }
                                        else
                                        {
                                            mainReg.BooleanOperation(BooleanOperationType.BoolUnite, reg1);
                                        }

                                    }

                                    else
                                    {

                                        // subtract the smaller region from the larger one

                                        reg2.BooleanOperation(BooleanOperationType.BoolUnite, reg1);
                                        if (!doUnion)
                                        {
                                            mainReg.BooleanOperation(BooleanOperationType.BoolSubtract, reg2);
                                        }
                                        else
                                        {
                                            mainReg.BooleanOperation(BooleanOperationType.BoolUnite, reg2);
                                        }
                                    }

                                }
                                // increase counter
                                i++;
                            } while (i < items.Length - 1);
                        }
                        mainReg.ColorIndex = 1;// put dummy color for region

                        // erase polylines 
                        foreach (Polyline poly in delPline)
                        {
                            if (poly != null)
                            {
                                if (!poly.IsWriteEnabled) poly.UpgradeOpen();
                                poly.Erase();
                                if (!poly.IsDisposed) poly.Dispose();
                            }
                        }

                        //  ---    explode region and create polyline from exploded entities   ---   //

                        DBObjectCollection regexpl = new DBObjectCollection();
                        mainReg.Explode(regexpl);

                        List<ObjectId> exids = new List<ObjectId>();

                        // gather selected object into the List<ObjectId>
                        if (regexpl.Count > 0)
                        {
                            foreach (DBObject obj in regexpl)
                            {
                                Entity ent = obj as Entity;
                                if (ent != null)
                                {
                                    ObjectId eid = btr.AppendEntity(ent);
                                    tr.AddNewlyCreatedDBObject(ent, true);

                                    exids.Add(eid);
                                }
                            }
                        }
                        // define AcadDocument as object
                        object ActiveDocument = doc.GetAcadDocument();
                        ObjectId[] entids = new ObjectId[] { };
                        Array.Resize(ref entids, exids.Count);
                        // convert List<ObjectId> to array of ObjectID
                        exids.CopyTo(entids, 0);

                        ed.Regen();
                        // create a new selection set and exploded items
                        SelectionSet newset = SelectionSet.FromObjectIds(entids);

                        ed.SetImpliedSelection(newset.GetObjectIds());

                        PromptSelectionResult pfres = ed.SelectImplied();
                        // execute Sendcommand synchronously
                        ActiveDocument.GetType().InvokeMember(
                            "SendCommand", System.Reflection.BindingFlags.InvokeMethod, null, ActiveDocument,
                            new object[] { "select\n" });
                        // execute Sendcommand synchronously 
                        string cmd = "_pedit _M _P" + " " + "" + " " + "_J" + " " + "" + " " + "" + "\n";
                        ActiveDocument.GetType().InvokeMember(
                            "SendCommand", System.Reflection.BindingFlags.InvokeMethod, null, ActiveDocument,
                            new object[] { cmd });
                        // rerase region if this is do not erased (relative to current DELOBJ variable value)
                        if (mainReg != null)
                            if (!mainReg.IsWriteEnabled)
                                mainReg.UpgradeOpen();
                        mainReg.Erase();

                        tr.Commit();
                    }

                    catch (Autodesk.AutoCAD.Runtime.Exception ex)
                    {

                        ed.WriteMessage("\nAutoCAD exception:\n" + ex.Message + "\n" + ex.StackTrace);
                    }
                    finally
                    {
                        ed.WriteMessage("\n{0}", new Autodesk.AutoCAD.Runtime.ErrorStatus().ToString());//optional, might be removed
                    }
                }
            }
        }
    }
}


