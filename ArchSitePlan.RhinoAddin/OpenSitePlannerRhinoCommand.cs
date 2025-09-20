using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI;
using ArchSitePlan.RhinoInterface.HTML_UI.Websockets;
using Eto.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using System;
using static ArchSitePlan.RhinoInterface.Constants.Enums;
using Command = Rhino.Commands.Command;

namespace ArchSitePlan.RhinoAddin;

public class OpenSitePlannerRhinoCommand : Command
{
    private static DisplayModeDescription _shadedMode = DisplayModeDescription.GetDisplayMode(DisplayModeDescription.ShadedId);
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

    private const int _httpsServerPort = 6300;
    private const int _webSocketServerPort = 6400;
    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        try
        {
            HttpServerComms.StartServer(_httpsServerPort);
            WebSocketComms.StartServer(_webSocketServerPort);
            var window = VueUI.OpenWindow();
            window.Closed += (sender, args) => HandleClosure(sender, args);
            UsageLogEmitter.RunPeriodicUsageLogEmitter();
            SetViewStyle();

            return Result.Success;
        }
        catch(Exception ex)
        {
            MessageBox.Show(ex.Message, "Failed to open plugin");
            return Result.Failure;
        }
        
    }

    private static void SetViewStyle()
    {
        var doc = RhinoDoc.ActiveDoc;
        doc.Views.ActiveView.ActiveViewport.DisplayMode = _shadedMode;
    }

    private void HandleClosure(object sender, EventArgs e)
    {
        try
        {
            HttpServerComms.StopServer();
            WebSocketComms.StopServer();
            RhinoEventHandler.UnsubscribeModelEvents();
            RhinoApp.WriteLine("AIQ Urban closed");
        }
        catch (Exception ex)
        {
            MessageBox.Show("AIQ Urban Plugin closure process incomplete. " +
                "Consider saving and re-opening your document to avoid instability");
        }
       
    }
}
