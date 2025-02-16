using WebSocketSharp;
using WebSocketSharp.Server;

namespace ArchSitePlan.UI.HTML_UI.Websockets;

internal class RhinoWebSocketBehavior : WebSocketBehavior
{
    // This method is called when the server receives a message from the client.
    protected override void OnMessage(MessageEventArgs e)
    {
        // Optionally, you can respond to client messages.
        Send("Server received: " + e.Data);
    }
}
