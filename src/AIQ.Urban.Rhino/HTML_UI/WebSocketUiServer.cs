using AIQ.Urban.RhinoInterface.MCP_Bridge;
using WebSocketSharp.Server;

namespace AIQ.Urban.RhinoInterface.HTML_UI;

public static class WebSocketUiServer
{
    private static WebSocketServer? _server;

    private const string BridgeHost = "127.0.0.1";

    public static void Start(int port)
    {
        if (_server?.IsListening == true)
            return;

        _server = new WebSocketServer($"ws://{BridgeHost}:{port}");
        _server.AddWebSocketService<UrbanUiWebSocketBehavior>("/");
        _server.AddWebSocketService<McpBridgeBehavior>("/mcp-bridge");
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
