using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Newtonsoft.Json;
using Rhino;
using WebSocketSharp.Server;

namespace ArchSitePlan.RhinoInterface.HTML_UI.Websockets;

public class WebSocketComms
{
    private static WebSocketServer? _wss = null;
    private static string _serverUrl;
    /// <summary>
    /// Starts the WebSocket server on the specified port.
    /// </summary>
    public static void StartServer(int port)
    {
        if (_wss?.IsListening?? false) return;

        _serverUrl = $"ws://localhost:{port}";
        _wss = new WebSocketServer(_serverUrl);
        _wss.Start();

        _wss.AddWebSocketService<RhinoWebSocketBehavior>("/");

        RhinoApp.WriteLine($"WebSocket Server is running on ws://localhost:{port}");
    }

    /// <summary>
    /// Broadcast a message containing osm tags to all connected WebSocket clients.
    /// </summary>
    public static void BroadcastMessage(IMessageToUI message)
    {
        if (_wss != null && _wss.IsListening)
        {
            string json = JsonConvert.SerializeObject(message);
            _wss.WebSocketServices.Broadcast(json);
            RhinoApp.WriteLine("Broadcasted message: "+ json);
        }
    }

    /// <summary>
    /// Stops the WebSocket server.
    /// </summary>
    public static void StopServer()
    {
        if (_wss != null)
        {
            _wss.Stop();
            RhinoApp.WriteLine("WebSocket Server stopped.");
        }
    }
}