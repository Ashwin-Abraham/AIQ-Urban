using System;
using System.Text.Json;
using AIQ.Urban.RhinoPlugin.MCP_Bridge;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using WebSocketSharp.Server;

namespace AIQ.Urban.RhinoPlugin.HTML_UI;

public static class WebSocketUiServer
{
    private static WebSocketServer? _server;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private static readonly object _selectionMessageLock = new();
    private static string _latestSelectionMessageJson =
        JsonSerializer.Serialize(SelectionMetricsMessage.Empty, JsonOptions);
    private static readonly object _modelStateMessageLock = new();
    private static string _latestModelStateMessageJson = "null";

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

    public static void Publish(SelectionMetricsMessage message)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);

        lock (_selectionMessageLock)
        {
            _latestSelectionMessageJson = json;
        }

        if (_server?.IsListening != true)
            return;

        _server.WebSocketServices["/"].Sessions.Broadcast(json);
    }

    internal static string LatestSelectionMessageJson()
    {
        lock (_selectionMessageLock)
        {
            return _latestSelectionMessageJson;
        }
    }

    public static void Publish(ModelStateMessage message)
    {
        var json = CacheModelState(message);

        if (_server?.IsListening != true)
            return;

        _server.WebSocketServices["/"].Sessions.Broadcast(json);
    }

    public static string CacheModelState(ModelStateMessage message)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);

        lock (_modelStateMessageLock)
        {
            _latestModelStateMessageJson = json;
        }

        return json;
    }

    internal static string LatestModelStateMessageJson()
    {
        lock (_modelStateMessageLock)
        {
            return _latestModelStateMessageJson;
        }
    }
}
