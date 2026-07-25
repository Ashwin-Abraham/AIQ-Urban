using Rhino;
using WebSocketSharp.Server;

namespace AIQ.Urban.RhinoPlugin.HTML_UI;

public class UrbanUiWebSocketBehavior : WebSocketBehavior
{
    protected override void OnOpen()
    {
        RhinoApp.WriteLine("AIQ Urban UI websocket connected.");
        Send(WebSocketUiServer.LatestSelectionMessageJson());
        Send(WebSocketUiServer.LatestModelStateMessageJson());
    }
}
