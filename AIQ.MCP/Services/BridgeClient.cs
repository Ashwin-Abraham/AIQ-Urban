using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace AIQ.MCP.Services;

internal class BridgeClient : IAsyncDisposable
{
    private ClientWebSocket? _ws;
    private readonly string _url;
    private int _nextRequestId;
    private readonly Dictionary<string, TaskCompletionSource<string>> _pending = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BridgeClient(string url)
    {
        _url = url;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _ws?.Dispose();
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(new Uri(_url), ct);

        _ = ReceiveLoopAsync();
    }

    public async Task<List<ToolDefinition>> ListToolsAsync(CancellationToken ct = default)
    {
        var requestId = Interlocked.Increment(ref _nextRequestId).ToString();
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = tcs;

        var request = new BridgeEnvelope("ListTools", requestId, null, null, null, null, null);
        var json = JsonSerializer.Serialize(request, JsonOptions);
        await SendAsync(json, ct);

        var responseJson = await tcs.Task.WaitAsync(ct);
        var response = JsonSerializer.Deserialize<BridgeEnvelope>(responseJson, JsonOptions);

        if (response?.Tools is JsonElement toolsEl)
        {
            return JsonSerializer.Deserialize<List<ToolDefinition>>(toolsEl.GetRawText(), JsonOptions) ?? [];
        }

        return [];
    }

    public async Task<JsonElement> CallToolAsync(string toolName, JsonElement arguments, CancellationToken ct = default)
    {
        var requestId = Interlocked.Increment(ref _nextRequestId).ToString();
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = tcs;

        var request = new BridgeEnvelope("ToolCall", requestId, toolName, arguments, null, null, null);
        var json = JsonSerializer.Serialize(request, JsonOptions);
        await SendAsync(json, ct);

        var responseJson = await tcs.Task.WaitAsync(ct);
        var response = JsonSerializer.Deserialize<BridgeEnvelope>(responseJson, JsonOptions);

        if (response?.Error is not null)
            throw new InvalidOperationException(response.Error);

        var content = response?.Content;
        if (content is null)
            return JsonDocument.Parse("[]").RootElement;

        return content.Value;
    }

    private async Task SendAsync(string message, CancellationToken ct)
    {
        if (_ws?.State != WebSocketState.Open)
            throw new InvalidOperationException("Bridge not connected");

        var bytes = Encoding.UTF8.GetBytes(message);
        await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
    }

    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[1024 * 64];
        var ws = _ws;
        if (ws is null) return;

        try
        {
            while (ws.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(buffer, CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        return;
                    }

                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                var text = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                using var doc = JsonDocument.Parse(text);
                var type = doc.RootElement.TryGetProperty("type", out var t) ? t.GetString() : null;
                var requestId = doc.RootElement.TryGetProperty("requestId", out var r) ? r.GetString() : null;

                if (requestId is not null && _pending.TryGetValue(requestId, out var tcs))
                {
                    _pending.Remove(requestId);
                    tcs.TrySetResult(text);
                }
            }
        }
        catch (WebSocketException wsEx)
        {
            Console.WriteLine($"[Bridge] WebSocket error: {wsEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Bridge] Receive error ({ex.GetType().Name}): {ex.Message}");
        }
        finally
        {
            foreach (var tcs in _pending.Values)
                tcs.TrySetException(new InvalidOperationException("Bridge disconnected"));

            _pending.Clear();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_ws is not null)
        {
            if (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.CloseReceived)
            {
                try { await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", CancellationToken.None); }
                catch { }
            }
            _ws.Dispose();
            _ws = null;
        }
    }
}
