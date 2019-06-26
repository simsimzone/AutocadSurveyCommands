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

using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutocadSurveyCommands
{
    public partial class AutocadSurveyCommands
    {
        static double defaultArea = 0.0;
        static double defaultOffset = 0.0;

        //Document doc;
        //Database db;
        //Editor ed;


        Document GetDocument()
        {
            return AcAp.DocumentManager.MdiActiveDocument;
        }

        /// <summary>
        /// Gets the MDI active docutment's editor.
        /// </summary>
        public static Editor ActiveEditor
        {
            get
            {
                return Application.DocumentManager.MdiActiveDocument.Editor;
            }
        }

        private (Polyline, Point3d?) SelectPolyline(Editor ed, Transaction tr,
            string message, string rejectMessage, bool closedNecessary)
        {
            PromptEntityOptions peo = new PromptEntityOptions(message)
            {
                AllowNone = false
            };
            peo.SetRejectMessage(rejectMessage);
            peo.AddAllowedClass(typeof(Polyline), true);
            PromptEntityResult per;
            Polyline pline;
            Point3d pickedPt;
            while (true)
            {
                per = ed.GetEntity(peo);
                if (per.Status == PromptStatus.Cancel)
                    return (null, null);
                pline = tr.GetObject(per.ObjectId, OpenMode.ForRead) as Polyline;
                if (per.Status == PromptStatus.OK
                    && pline != null
                    && closedNecessary ? pline.Closed : true)
                    break;
            }
            pickedPt = pline.GetClosestPointTo(per.PickedPoint, true);
            return (pline, pickedPt);
        }

        private Point3d?GetPoint3D(Editor ed, Transaction tr,
            string message, string rejectMessage)
        {
            PromptPointOptions ppo = new PromptPointOptions(message)
            {
                AllowNone = false
            };

            PromptPointResult ppr;

            while (true)
            {
                ppr = ed.GetPoint(ppo);
                if (ppr.Status == PromptStatus.OK)
                    break;
                if (ppr.Status == PromptStatus.Cancel)
                    return null;
            }
            return ppr.Value;
        }
    }
}
