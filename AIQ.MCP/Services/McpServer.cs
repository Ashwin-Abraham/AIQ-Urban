using System.Diagnostics;
using System.Net;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace AIQ.MCP.Services;

public sealed class McpServer : IAsyncDisposable, IDisposable
{
    private HttpListener? _listener;
    private StreamableHttpServerTransport? _transport;
    private ModelContextProtocol.Server.McpServer? _server;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private readonly string _portFilePath;
    private int _port;

    public int Port => _port;
    public bool IsRunning => _listener?.IsListening ?? false;

    public McpServer()
    {
        _portFilePath = Path.Combine(Path.GetTempPath(), "AIQ.MCP.port");
    }

    public async Task Start(int preferredPort = 0)
    {
        await StopAsync().ConfigureAwait(false);

        var port = preferredPort;
        if (port == 0)
        {
            using var tcpListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            tcpListener.Start();
            port = ((System.Net.IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
        }

        _port = port;
        _cts = new CancellationTokenSource();

        try
        {
            _transport = new StreamableHttpServerTransport(loggerFactory: null);

            var options = new McpServerOptions
            {
                ServerInfo = new Implementation
                {
                    Name = "AIQ.MCP",
                    Version = "1.0.0"
                },
                Capabilities = new ServerCapabilities
                {
                    Tools = new ToolsCapability()
                }
            };

            _server = ModelContextProtocol.Server.McpServer.Create(
                _transport, options, loggerFactory: null, serviceProvider: null);

            _ = _server.RunAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[McpServer] MCP init failed: {ex}");
            throw;
        }

        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[McpServer] HttpListener failed: {ex}");
            throw;
        }

        await File.WriteAllTextAsync(_portFilePath, port.ToString()).ConfigureAwait(false);

        _listenTask = AcceptRequestsAsync(_cts.Token);
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();

        // Stop listener BEFORE awaiting listenTask so GetContextAsync completes
        var listener = _listener;
        if (listener is not null)
        {
            try { listener.Stop(); } catch { }
        }

        if (_listenTask is not null)
        {
            try { await _listenTask.ConfigureAwait(false); } catch (OperationCanceledException) { }
            catch (NullReferenceException) { }
            catch (ObjectDisposedException) { }
            catch (HttpListenerException) { }
        }

        if (listener is not null)
        {
            listener.Close();
        }

        if (_server is not null)
        {
            await _server.DisposeAsync().ConfigureAwait(false);
            _server = null;
        }

        if (_transport is not null)
        {
            await _transport.DisposeAsync().ConfigureAwait(false);
            _transport = null;
        }

        try
        {
            if (File.Exists(_portFilePath))
                File.Delete(_portFilePath);
        }
        catch { }

        _listener = null;
        _cts?.Dispose();
        _cts = null;
    }

    private async Task AcceptRequestsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener?.IsListening == true)
        {
            try
            {
                var listener = _listener;
                if (listener is null) break;
                var context = await listener.GetContextAsync().WaitAsync(ct).ConfigureAwait(false);
                _ = HandleRequestAsync(context, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (HttpListenerException) { break; }
            catch (NullReferenceException) { break; }
            catch (ObjectDisposedException) { break; }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "GET")
            {
                response.ContentType = "text/event-stream";
                response.Headers.Add("Cache-Control", "no-cache");
                response.Headers.Add("Connection", "keep-alive");
                await _transport!.HandleGetRequestAsync(response.OutputStream, ct).ConfigureAwait(false);
            }
            else if (request.HttpMethod == "POST")
            {
                using var reader = new StreamReader(request.InputStream);
                var json = await reader.ReadToEndAsync().ConfigureAwait(false);
                var message = JsonSerializer.Deserialize<JsonRpcMessage>(json);

                using var memStream = new MemoryStream();
                var handled = await _transport!.HandlePostRequestAsync(message, memStream, ct).ConfigureAwait(false);

                if (handled)
                {
                    memStream.Position = 0;
                    response.ContentType = "application/json";
                    response.StatusCode = 200;
                    await memStream.CopyToAsync(response.OutputStream).ConfigureAwait(false);
                }
                else
                {
                    response.StatusCode = 202;
                }
            }
            else
            {
                response.StatusCode = 405;
            }
        }
        catch (Exception ex)
        {
            try
            {
                context.Response.StatusCode = 500;
                await using var writer = new StreamWriter(context.Response.OutputStream);
                await writer.WriteAsync($"{{\"error\":\"{ex.Message}\"}}").ConfigureAwait(false);
            }
            catch { }
        }
        finally
        {
            try { context.Response.OutputStream.Close(); } catch { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
    }
}
