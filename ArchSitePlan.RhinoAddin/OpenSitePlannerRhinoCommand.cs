using System;
using ArchSitePlan.RhinoInterface.Actions;
using ArchSitePlan.RhinoInterface.HTML_UI;
using ArchSitePlan.RhinoInterface.HTML_UI.Websockets;
using Eto.Forms;
using Rhino;
using Rhino.Commands;
using Command = Rhino.Commands.Command;

namespace ArchSitePlan.RhinoAddin
{
    public class OpenSitePlannerRhinoCommand : Command
    {
        public OpenSitePlannerRhinoCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static OpenSitePlannerRhinoCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "AIQ_Urban";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                //Rhino.UI.Panels.OpenPanel(MainViewHost.PanelId);
                //RhinoApp.WriteLine("Loaded plugin");
                ServerComms.StartServer();
                WebSocketComms.StartServer();
                VueUI.OpenWindow();
                RhinoEventHandler.AppUsageLog = new();

                return Result.Success;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to open plugin");
                return Result.Failure;
            }
            
        }
    }
}
