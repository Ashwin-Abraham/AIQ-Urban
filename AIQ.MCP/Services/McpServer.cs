using System.Diagnostics;
using System.Net;
using System.Text.Json;
using AIQ.MCP.Models;
using AIQ.Urban.Core;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Pipeline.Configs;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Rhino;

namespace AIQ.MCP.Services;

public sealed class McpServer : IAsyncDisposable, IDisposable
{
    private static readonly JsonSerializerOptions McpJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

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

    private static JsonElement MakeToolSchema(string schemaJson)
        => JsonSerializer.Deserialize<JsonElement>(schemaJson);

    private static readonly JsonElement EmptySchema = MakeToolSchema("""
        {
            "type": "object",
            "properties": {},
            "required": []
        }
        """);

    private static readonly JsonElement AddBuildingSchema = MakeToolSchema("""
        {
            "type": "object",
            "properties": {
                "items": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "footprint": {
                                "type": "array",
                                "items": {
                                    "type": "array",
                                    "items": { "type": "number" },
                                    "minItems": 2,
                                    "maxItems": 2
                                },
                                "minItems": 3
                            },
                            "typicalFloorHeight": { "type": "number", "exclusiveMinimum": 0 },
                            "floorCount": { "type": "integer", "exclusiveMinimum": 0 },
                            "useName": { "type": "string" }
                        },
                        "required": ["footprint", "typicalFloorHeight", "floorCount", "useName"]
                    },
                    "minItems": 1
                }
            },
            "required": ["items"]
        }
        """);

    private static readonly JsonElement AddTransitSchema = MakeToolSchema("""
        {
            "type": "object",
            "properties": {
                "items": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "centerline": {
                                "type": "array",
                                "items": {
                                    "type": "array",
                                    "items": { "type": "number" },
                                    "minItems": 2,
                                    "maxItems": 2
                                },
                                "minItems": 2
                            },
                            "width": { "type": "number", "exclusiveMinimum": 0 },
                            "transitType": { "type": "string" }
                        },
                        "required": ["centerline", "width", "transitType"]
                    },
                    "minItems": 1
                }
            },
            "required": ["items"]
        }
        """);

    private static readonly JsonElement ChangeBuildingUseSchema = MakeToolSchema("""
        {
            "type": "object",
            "properties": {
                "items": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "buildingGeometryId": {
                                "type": "string",
                                "format": "uuid",
                                "description": "GUID of the building envelope to change"
                            },
                            "newUseName": {
                                "type": "string",
                                "description": "New use name (e.g. Residential, Commercial)"
                            }
                        },
                        "required": ["buildingGeometryId", "newUseName"]
                    },
                    "minItems": 1
                }
            },
            "required": ["items"]
        }
        """);

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
                },
                Handlers = new McpServerHandlers
                {
                    ListToolsHandler = (_, _) =>
                        ValueTask.FromResult(new ListToolsResult
                        {
                            Tools =
                            [
                                new Tool
                                {
                                    Name = "list_elements",
                                    Description = "Lists all urban elements (transits, buildings, parcels, zones) in the current model. Returns structured JSON summaries with GUIDs, types, uses, and type-specific details for each element.",
                                    InputSchema = EmptySchema
                                },
                                new Tool
                                {
                                    Name = "add_building",
                                    Description = "Adds one or more buildings from footprint coordinates. Accepts an array of building items, each with footprint (closed double[][]), typicalFloorHeight, floorCount, and useName. Returns the GUIDs of created geometry. Triggers full pipeline cascade including metrics calculation.",
                                    InputSchema = AddBuildingSchema
                                },
                                new Tool
                                {
                                    Name = "add_transit",
                                    Description = "Adds one or more transit corridors from centerline coordinates. Accepts an array of transit items, each with centerline (double[][]), width, and transitType. Returns the GUIDs of created geometry. Triggers full pipeline cascade including transit surface union, parcel recalculation, and metrics calculation.",
                                    InputSchema = AddTransitSchema
                                },
                                new Tool
                                {
                                    Name = "change_building_use",
                                    Description = "Changes the use of one or more buildings. Accepts an array of change items, each with buildingGeometryId (GUID) and newUseName (string). Returns the GUIDs of affected buildings. Triggers metrics recalculation.",
                                    InputSchema = ChangeBuildingUseSchema
                                }
                            ]
                        }),

                    CallToolHandler = (request, _) =>
                    {
                        if (request.Params?.Name == "list_elements")
                        {
                            var service = new UrbanModelService();
                            var elements = service.GetElementSummaries();
                            var json = ElementSummarySerializer.Serialize(elements);
                            return ValueTask.FromResult(new CallToolResult
                            {
                                Content = [new TextContentBlock { Text = json }]
                            });
                        }

                        if (request.Params?.Name == "add_building")
                        {
                            return ValueTask.FromResult(HandleAddBuilding(request));
                        }

                        if (request.Params?.Name == "add_transit")
                        {
                            return ValueTask.FromResult(HandleAddTransit(request));
                        }

                        if (request.Params?.Name == "change_building_use")
                        {
                            return ValueTask.FromResult(HandleChangeBuildingUse(request));
                        }

                        var available = string.Join(", ", "list_elements", "add_building", "add_transit", "change_building_use");
                        throw new McpProtocolException(
                            $"Unknown tool: '{request.Params?.Name}'. Available tools: {available}",
                            McpErrorCode.InvalidParams);
                    }
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

    private static CallToolResult HandleAddBuilding(RequestContext<CallToolRequestParams> request)
    {
        _ = request ?? throw new ArgumentNullException(nameof(request));

        // Deserialize arguments
        var argsJson = JsonSerializer.Serialize(request.Params?.Arguments);
        var addRequest = JsonSerializer.Deserialize<AddBuildingRequest>(argsJson, McpJsonOptions);

        if (addRequest?.Items == null || addRequest.Items.Count == 0)
        {
            throw new McpProtocolException(
                "add_building requires at least one building item with footprint, typicalFloorHeight, floorCount, and useName.",
                McpErrorCode.InvalidParams);
        }

        // Create pipeline configuration (validates input — throws on invalid coords, unknown use, etc.)
        var service = new UrbanModelService();
        var config = service.CreateBuildingSteps(addRequest);

        // Execute pipeline (requires active Rhino doc)
        var doc = RhinoDoc.ActiveDoc;
        if (doc == null)
        {
            throw new McpProtocolException(
                "Active Rhino document is required for add_building. Ensure the MCP server is running within Rhino.",
                McpErrorCode.InternalError);
        }

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
        new PipelineConfigurations(pipeline)
            .WithBuildingAdditionSteps(config)
            .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name);
        pipeline.Run();

        doc.Views.Redraw();

        // Build result with created geometry GUIDs
        var elementGuids = config.InputCurveIds.Select(id => id.ToString()).ToList();
        var resultJson = JsonSerializer.Serialize(
            new { elementGuids }, McpJsonOptions);

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = resultJson }]
        };
    }

    private static CallToolResult HandleAddTransit(RequestContext<CallToolRequestParams> request)
    {
        _ = request ?? throw new ArgumentNullException(nameof(request));

        // Deserialize arguments
        var argsJson = JsonSerializer.Serialize(request.Params?.Arguments);
        var addRequest = JsonSerializer.Deserialize<AddTransitRequest>(argsJson, McpJsonOptions);

        if (addRequest?.Items == null || addRequest.Items.Count == 0)
        {
            throw new McpProtocolException(
                "add_transit requires at least one transit item with centerline, width, and transitType.",
                McpErrorCode.InvalidParams);
        }

        // Create pipeline configuration (validates input — throws on invalid coords, unknown type, etc.)
        var service = new UrbanModelService();
        var config = service.CreateTransitSteps(addRequest);

        // Execute pipeline (requires active Rhino doc)
        var doc = RhinoDoc.ActiveDoc;
        if (doc == null)
        {
            throw new McpProtocolException(
                "Active Rhino document is required for add_transit. Ensure the MCP server is running within Rhino.",
                McpErrorCode.InternalError);
        }

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
        new PipelineConfigurations(pipeline)
            .WithTransitAdditionSteps(config)
            .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name);
        pipeline.Run();

        doc.Views.Redraw();

        // Build result with created geometry GUIDs
        var elementGuids = config.CenterlineIds.Select(id => id.ToString()).ToList();
        var resultJson = JsonSerializer.Serialize(
            new { elementGuids }, McpJsonOptions);

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = resultJson }]
        };
    }

    private static CallToolResult HandleChangeBuildingUse(RequestContext<CallToolRequestParams> request)
    {
        _ = request ?? throw new ArgumentNullException(nameof(request));

        // Deserialize arguments
        var argsJson = JsonSerializer.Serialize(request.Params?.Arguments);
        var changeRequest = JsonSerializer.Deserialize<ChangeBuildingUseRequest>(argsJson, McpJsonOptions);

        if (changeRequest?.Items == null || changeRequest.Items.Count == 0)
        {
            throw new McpProtocolException(
                "change_building_use requires at least one change item with buildingGeometryId and newUseName.",
                McpErrorCode.InvalidParams);
        }

        // Create the change steps (validates input — throws on empty GUID, unknown use, etc.)
        var service = new UrbanModelService();
        var steps = service.CreateChangeUseSteps(changeRequest);

        // Execute pipeline (requires active Rhino doc)
        var doc = RhinoDoc.ActiveDoc;
        if (doc == null)
        {
            throw new McpProtocolException(
                "Active Rhino document is required for change_building_use. Ensure the MCP server is running within Rhino.",
                McpErrorCode.InternalError);
        }

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
        foreach (var step in steps)
        {
            pipeline.WithUrbanElementStep(step);
        }
        new PipelineConfigurations(pipeline)
            .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name);
        pipeline.Run();

        doc.Views.Redraw();

        // Build result with affected building GUIDs
        var affectedGuids = changeRequest.Items
            .Select(i => i.BuildingGeometryId.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var resultJson = JsonSerializer.Serialize(
            new { affectedGuids }, McpJsonOptions);

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = resultJson }]
        };
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
