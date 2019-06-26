using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GI = Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(
  typeof(AutocadSurveyCommands.AutocadSurveyCommands)
)]


namespace AutocadSurveyCommands
{

    public partial class AutocadSurveyCommands
    {
        [CommandMethod("XXKW")]
        public void KeywordTest()
        {
            Document doc = GetDocument();
            Database db = doc.Database;
            Editor ed = doc.Editor;

            while (true)
            {
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        var requiredArea = GetUserArea(ed, "\nSpecify required Area");
                        if (requiredArea.HasValue)
                        {
                            ed.WriteMessage("\n You entered " + requiredArea);
                        }
                        else
                            return;
                        trans.Commit();

                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception ex)
                    {
                        ed.WriteMessage(ex.Message + ex.StackTrace);
                    }
                    finally
                    {

                    }
                }
            }
        }

        private static List<double> defaultAreas = null;
        private double? GetUserArea(Editor ed, string message)
        {

            double requiredArea = 0.0;
            //manipulate defaultArea
            PromptDoubleOptions pdo = new PromptDoubleOptions(message)
            {
                AllowNegative = false,
                AllowNone = false,
                AllowZero = false,
                DefaultValue = defaultArea,
                UseDefaultValue = defaultArea == 0 ? false : true
            };
            if (defaultAreas != null && defaultAreas.Count > 0)
            {
                pdo.Message += " or: ";
                foreach (var area in defaultAreas)
                {
                    pdo.Keywords.Add(area.ToString());
                }
            }
            PromptDoubleResult pdr = ed.GetDouble(pdo);
            if (pdr.Status != PromptStatus.OK)
                return null;

            requiredArea = pdr.Value;
            if (defaultArea != 0 && defaultArea != requiredArea)
            {
                // Here we have to insert the old def value to the store
                if (defaultAreas == null)
                    defaultAreas = new List<double>();
                // if defaultAreas has the requiredArea then remove it because it
                // will be set as a default value.
                if (defaultAreas.Contains(requiredArea))
                {
                    defaultAreas.Remove(requiredArea);
                }
                if (!defaultAreas.Contains(defaultArea))
                {
                    // too large
                    if (defaultAreas.Count >= 5)
                    {
                        // remove the oldest area
                        defaultAreas.RemoveAt(0);
                    }
                    defaultAreas.Add(defaultArea);
                }
                
            }
            defaultArea = requiredArea;
            return requiredArea;
        }
    }
}
