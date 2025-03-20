using Rhino;
using Rhino.Commands;

namespace ArchSitePlan.RhinoInterface.HTML_UI.Websockets;

public class ExampleEventCommand : Command
{
    public ExampleEventCommand()
    {
        Instance = this;
        // Start the WebSocket server when the command is constructed.
        WebSocketComms.StartServer();
    }

    public static ExampleEventCommand Instance { get; private set; }

    public override string EnglishName => "ExampleEventCommand";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        RhinoApp.WriteLine("Add objects in Rhino to trigger a broadcast event.");

        // Trigger an event as a result of code flow reaching this point.
        // Make that event broadcast a message to the vue js ui via the websocket thing we#re setting up
        string message = "RunCommand triggered broadcast at " + DateTime.Now.ToString("T");
        WebSocketComms.BroadcastMessage(message);

        return Result.Success;
    }
}
