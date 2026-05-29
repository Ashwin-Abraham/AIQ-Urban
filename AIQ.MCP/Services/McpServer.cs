using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;
using Rhino;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace AIQ.MCP.Services;

public class McpServer(RhinoDoc doc, UrbanModel model) : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private TcpListener? _listener;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listenerTask;

    public int? Port { get; private set; }

    public Task Start(int preferredPort = 0)
    {
        if (_listener is not null)
            return Task.CompletedTask;

        Port = preferredPort == 0 ? FindFreeLoopbackPort() : preferredPort;
        _cancellationTokenSource = new CancellationTokenSource();

        _listener = new TcpListener(IPAddress.Loopback, Port.Value);
        _listener.Start();
        _listenerTask = Listen(_cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    public async Task Stop()
    {
        if (_listener is null)
            return;

        _cancellationTokenSource?.Cancel();
        _listener.Stop();

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _listener = null;
        _listenerTask = null;
        Port = null;
    }

    private async Task Listen(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener is not null)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (InvalidOperationException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }

            _ = Task.Run(() => Handle(client, cancellationToken), cancellationToken);
        }
    }

    private async Task Handle(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            var stream = client.GetStream();
            var request = await ReadHttpRequest(stream, cancellationToken);
            var response = request is null
                ? CreateHttpResponse(400, "text/plain", "Bad Request"u8.ToArray())
                : await HandleHttpRequest(request.Value, cancellationToken);

            await stream.WriteAsync(response, cancellationToken);
        }
    }

    private async Task<byte[]> HandleHttpRequest(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.Method == "GET" && request.Path == "/")
            return CreateHttpResponse(200, "text/plain; charset=utf-8", Encoding.UTF8.GetBytes("AIQ Urban MCP server is running."));

        if (!string.Equals(request.Path, "/mcp", StringComparison.OrdinalIgnoreCase))
            return CreateHttpResponse(404, "text/plain", "Not Found"u8.ToArray());

        if (request.Method != "POST")
            return CreateHttpResponse(405, "text/plain", "Method Not Allowed"u8.ToArray());

        try
        {
            using var json = JsonDocument.Parse(request.Body);
            var jsonRpc = HandleJsonRpc(json.RootElement);
            return CreateHttpResponse(jsonRpc.StatusCode, jsonRpc.ContentType, jsonRpc.Body);
        }
        catch (JsonException ex)
        {
            var body = CreateJsonRpcError(null, -32700, ex.Message);
            return CreateHttpResponse(200, "application/json", body);
        }
    }

    private JsonRpcHttpResponse HandleJsonRpc(JsonElement request)
    {
        var id = request.TryGetProperty("id", out var idElement) ? idElement.Clone() : (JsonElement?)null;
        var method = request.TryGetProperty("method", out var methodElement) ? methodElement.GetString() : null;

        if (string.IsNullOrWhiteSpace(method))
            return JsonRpcResponse(CreateJsonRpcError(id, -32600, "Missing JSON-RPC method."));

        if (id is null && method.StartsWith("notifications/", StringComparison.OrdinalIgnoreCase))
            return new JsonRpcHttpResponse(202, "application/json", []);

        try
        {
            return method switch
            {
                "initialize" => JsonRpcResponse(CreateJsonRpcResult(id, CreateInitializeResult())),
                "tools/list" => JsonRpcResponse(CreateJsonRpcResult(id, CreateToolsListResult())),
                "tools/call" => JsonRpcResponse(CreateJsonRpcResult(id, CallTool(request))),
                _ => JsonRpcResponse(CreateJsonRpcError(id, -32601, $"Unknown method '{method}'."))
            };
        }
        catch (Exception ex)
        {
            return JsonRpcResponse(CreateJsonRpcError(id, -32603, ex.Message));
        }
    }

    private object CreateInitializeResult()
    {
        return new
        {
            protocolVersion = "2025-06-18",
            capabilities = new
            {
                tools = new { }
            },
            serverInfo = new
            {
                name = "aiq-urban-rhino",
                title = "AIQ Urban Rhino",
                version = "1.0"
            }
        };
    }

    private object CreateToolsListResult()
    {
        return new
        {
            tools = new object[]
            {
                new
                {
                    name = "add_transit",
                    title = "Add transit by drawing a curve",
                    description = "Adds transits to an urban model from centerline coordinates, transit width, and transit type.",
                    inputSchema = new
                    {
                        type = "object",
                        required = new[] { "width", "type", "curveCoordinates" },
                        properties = new
                        {
                            width = new { type = "number" },
                            type = new { type = "string", @enum = Enum.GetNames<TransitType>() },
                            curveCoordinates = CurveCoordinatesSchema()
                        }
                    }
                },
                new
                {
                    name = "add_building",
                    title = "Generate Building",
                    description = "Adds buildings to an urban model from footprint coordinates, floor height, floor count, and use.",
                    inputSchema = new
                    {
                        type = "object",
                        required = new[] { "floorHeight", "floorCount", "use", "footprintCurves" },
                        properties = new
                        {
                            floorHeight = new { type = "number" },
                            floorCount = new { type = "integer" },
                            use = new { type = "string", @enum = DefaultUses.Names },
                            footprintCurves = CurveCoordinatesSchema()
                        }
                    }
                }
            }
        };
    }

    private static object CurveCoordinatesSchema()
    {
        return new
        {
            type = "array",
            items = new
            {
                type = "object",
                properties = new
                {
                    coordinates = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "array",
                            minItems = 2,
                            maxItems = 2,
                            items = new { type = "number" }
                        }
                    }
                }
            }
        };
    }

    private object CallTool(JsonElement request)
    {
        if (!request.TryGetProperty("params", out var paramsElement))
            throw new ArgumentException("tools/call requires params.");

        var name = paramsElement.GetProperty("name").GetString();
        var arguments = paramsElement.TryGetProperty("arguments", out var argumentsElement)
            ? argumentsElement
            : throw new ArgumentException("tools/call requires arguments.");

        var tools = new UrbanModelTools(doc, model);
        var result = name switch
        {
            "add_transit" => tools.AddTransit(
                arguments.GetProperty("width").GetDouble(),
                Enum.Parse<TransitType>(arguments.GetProperty("type").GetString()!, ignoreCase: true),
                ReadCurveCoordinates(arguments.GetProperty("curveCoordinates"))),
            "add_building" => tools.AddBuilding(
                arguments.GetProperty("floorHeight").GetDouble(),
                arguments.GetProperty("floorCount").GetInt32(),
                ReadUse(arguments.GetProperty("use")),
                ReadCurveCoordinates(arguments.GetProperty("footprintCurves"))),
            _ => throw new ArgumentException($"Unknown tool '{name}'.")
        };

        return new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = JsonSerializer.Serialize(result, JsonOptions)
                }
            },
            isError = false
        };
    }

    private static Use ReadUse(JsonElement useElement)
    {
        if (useElement.ValueKind == JsonValueKind.String)
            return DefaultUses.Resolve(useElement.GetString()!);

        if (useElement.ValueKind == JsonValueKind.Object && useElement.TryGetProperty("name", out var nameElement))
            return DefaultUses.Resolve(nameElement.GetString()!);

        throw new ArgumentException("Use must be a known use name or an object with a name property.");
    }

    private static List<CurveCoordinates> ReadCurveCoordinates(JsonElement curvesElement)
    {
        var curves = new List<CurveCoordinates>();

        foreach (var curveElement in curvesElement.EnumerateArray())
        {
            var coordinatesElement = curveElement.GetProperty("coordinates");
            var coordinates = new List<(double X, double Y)>();

            foreach (var pointElement in coordinatesElement.EnumerateArray())
                coordinates.Add(ReadPoint(pointElement));

            curves.Add(new CurveCoordinates(coordinates));
        }

        return curves;
    }

    private static (double X, double Y) ReadPoint(JsonElement pointElement)
    {
        if (pointElement.ValueKind == JsonValueKind.Array)
        {
            var values = pointElement.EnumerateArray().ToArray();
            return (values[0].GetDouble(), values[1].GetDouble());
        }

        return (pointElement.GetProperty("x").GetDouble(), pointElement.GetProperty("y").GetDouble());
    }

    private static byte[] CreateJsonRpcResult(JsonElement? id, object result)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        writer.WriteString("jsonrpc", "2.0");
        writer.WritePropertyName("id");
        WriteId(writer, id);
        writer.WritePropertyName("result");
        JsonSerializer.Serialize(writer, result, JsonOptions);
        writer.WriteEndObject();
        writer.Flush();
        return stream.ToArray();
    }

    private static byte[] CreateJsonRpcError(JsonElement? id, int code, string message)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        writer.WriteStartObject();
        writer.WriteString("jsonrpc", "2.0");
        writer.WritePropertyName("id");
        WriteId(writer, id);
        writer.WritePropertyName("error");
        writer.WriteStartObject();
        writer.WriteNumber("code", code);
        writer.WriteString("message", message);
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();
        return stream.ToArray();
    }

    private static void WriteId(Utf8JsonWriter writer, JsonElement? id)
    {
        if (id is { } idElement)
            idElement.WriteTo(writer);
        else
            writer.WriteNullValue();
    }

    private static JsonRpcHttpResponse JsonRpcResponse(byte[] body)
    {
        return new JsonRpcHttpResponse(200, "application/json", body);
    }

    private static async Task<HttpRequest?> ReadHttpRequest(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        using var requestBytes = new MemoryStream();
        var headerEnd = -1;

        while (headerEnd < 0 && requestBytes.Length < 1024 * 1024)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                return null;

            requestBytes.Write(buffer, 0, read);
            headerEnd = IndexOf(requestBytes.GetBuffer(), (int)requestBytes.Length, "\r\n\r\n"u8.ToArray());
        }

        if (headerEnd < 0)
            return null;

        var allBytes = requestBytes.ToArray();
        var headerText = Encoding.ASCII.GetString(allBytes, 0, headerEnd);
        var headerLines = headerText.Split("\r\n");
        var requestLine = headerLines[0].Split(' ');

        if (requestLine.Length < 2)
            return null;

        var contentLength = 0;
        foreach (var line in headerLines.Skip(1))
        {
            var separator = line.IndexOf(':');
            if (separator < 0)
                continue;

            var name = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
                contentLength = int.Parse(value);
        }

        var bodyStart = headerEnd + 4;
        var body = new byte[contentLength];
        var bufferedBodyLength = Math.Min(contentLength, allBytes.Length - bodyStart);
        Array.Copy(allBytes, bodyStart, body, 0, bufferedBodyLength);

        var remaining = contentLength - bufferedBodyLength;
        var offset = bufferedBodyLength;
        while (remaining > 0)
        {
            var read = await stream.ReadAsync(body.AsMemory(offset, remaining), cancellationToken);
            if (read == 0)
                break;

            offset += read;
            remaining -= read;
        }

        var path = requestLine[1].Split('?')[0];
        return new HttpRequest(requestLine[0], path, body);
    }

    private static int IndexOf(byte[] buffer, int length, byte[] value)
    {
        for (var i = 0; i <= length - value.Length; i++)
        {
            var found = true;
            for (var j = 0; j < value.Length; j++)
            {
                if (buffer[i + j] != value[j])
                {
                    found = false;
                    break;
                }
            }

            if (found)
                return i;
        }

        return -1;
    }

    private static byte[] CreateHttpResponse(int statusCode, string contentType, byte[] body)
    {
        var reason = statusCode switch
        {
            200 => "OK",
            202 => "Accepted",
            400 => "Bad Request",
            404 => "Not Found",
            405 => "Method Not Allowed",
            _ => "OK"
        };

        var headers = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {statusCode} {reason}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {body.Length}\r\n" +
            "Connection: close\r\n" +
            "\r\n");

        return [.. headers, .. body];
    }

    private static int FindFreeLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task IgnoreCancellation(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (TimeoutException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Stop();
    }

    private readonly record struct HttpRequest(string Method, string Path, byte[] Body);

    private readonly record struct JsonRpcHttpResponse(int StatusCode, string ContentType, byte[] Body);
}
