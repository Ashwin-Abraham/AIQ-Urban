using WebSocketSharp.Server;

namespace AIQ.Urban.RhinoInterface.HTML_UI;

public static class WebSocketUiServer
{
    private static WebSocketServer? _server;

    public static void Start(int port)
    {
        if (_server?.IsListening == true)
            return;

        _server = new WebSocketServer($"ws://localhost:{port}");
        _server.AddWebSocketService<UrbanUiWebSocketBehavior>("/");
        _server.Start();
    }

    public static void Stop()
    {
        if (_server is null)
            return;

        _server.Stop();
        _server = null;
    }
}
