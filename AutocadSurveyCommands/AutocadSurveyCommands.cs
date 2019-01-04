using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutocadSurveyCommands
{
    public partial class AutocadSurveyCommands
    {
        static double defaultArea = 0.0;
        //static double defaultOffset = 0.0;

        //Document doc;
        //Database db;
        //Editor ed;


        Document GetDocument()
        {
            return AcAp.DocumentManager.MdiActiveDocument;
        }
    }
}
