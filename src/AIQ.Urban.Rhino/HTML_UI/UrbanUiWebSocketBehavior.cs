using Rhino;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace AIQ.Urban.RhinoInterface.HTML_UI;

public class UrbanUiWebSocketBehavior : WebSocketBehavior
{
    protected override void OnOpen()
    {
        RhinoApp.WriteLine("AIQ Urban UI websocket connected.");
    }
}
