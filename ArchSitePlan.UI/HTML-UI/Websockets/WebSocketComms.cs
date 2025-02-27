using ArchSitePlan.UI.HTML_UI.MessageToUI;
using Newtonsoft.Json;
using Rhino;
using WebSocketSharp.Server;

namespace ArchSitePlan.UI.HTML_UI.Websockets;

public class WebSocketComms
{
    private static WebSocketServer _wss;

    /// <summary>
    /// Starts the WebSocket server on the specified port.
    /// </summary>
    public static void StartServer(int port = 6400)
    {
        // Create a new WebSocket server listening on ws://localhost:port
        _wss = new WebSocketServer($"ws://localhost:{port}");

        // Add a WebSocket service at the root URL
        _wss.AddWebSocketService<RhinoWebSocketBehavior>("/");

        _wss.Start();
        RhinoApp.WriteLine($"WebSocket Server is running on ws://localhost:{port}");
    }

    /// <summary>
    /// Broadcast a message to all connected WebSocket clients.
    /// </summary>
    public static void BroadcastMessage(string message)
    {
        if (_wss != null && _wss.IsListening)
        {
            _wss.WebSocketServices.Broadcast(message);
            RhinoApp.WriteLine("Broadcasted message: " + message);
        }
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
            RhinoApp.WriteLine("Broadcasted message: osmTags");
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