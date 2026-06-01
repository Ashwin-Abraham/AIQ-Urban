using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.Metrics;
using Rhino;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace AIQ.Urban.RhinoInterface.MCP_Bridge;

internal class McpBridgeBehavior : WebSocketBehavior
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private int _requestCounter;

    protected override void OnMessage(MessageEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.Data);
            var root = doc.RootElement;

            var type = root.TryGetProperty("type", out var t) ? t.GetString() : null;
            var requestId = root.TryGetProperty("requestId", out var r) ? r.GetString() : null;

            switch (type)
            {
                case "ListTools":
                    HandleListTools(requestId);
                    break;

                case "ToolCall":
                    var toolName = root.TryGetProperty("tool", out var tn) ? tn.GetString() : null;
                    var arguments = root.TryGetProperty("arguments", out var args) ? args : (JsonElement?)null;
                    HandleToolCall(requestId, toolName, arguments);
                    break;
            }
        }
        catch (Exception ex)
        {
            SendError(null, $"Bridge error: {ex.Message}");
        }
    }

    private void HandleListTools(string? requestId)
    {
        var tools = new JsonArray
        {
            BuildToolSchema("add_transit", "Adds transits by centerline coordinates, width, and transit type.",
                new { type = "object", properties = new { width = new { type = "number", description = "Width of transit in model units" }, type = new { type = "string", description = "Type of transit", @enum = new[] { "Road", "Rail", "CyclePath", "Pedestrian" } }, curveCoordinates = new { type = "array", description = "Centerline XY coordinate arrays", items = new { type = "object", properties = new { coordinates = new { type = "array", items = new { type = "array", items = new { type = "number" }, minItems = 2, maxItems = 2 } } } } } }, required = new[] { "width", "type", "curveCoordinates" } }),

            BuildToolSchema("add_building", "Adds buildings by footprint coordinates, floor height, floor count, and use type.",
                new { type = "object", properties = new { floorHeight = new { type = "number", description = "Floor to floor height" }, floorCount = new { type = "integer", description = "Number of floors" }, useName = new { type = "string", description = "Use type (e.g. Residential, Commercial)" }, footprintCurves = new { type = "array", description = "Footprint XY coordinate arrays (closed rings)", items = new { type = "object", properties = new { coordinates = new { type = "array", items = new { type = "array", items = new { type = "number" }, minItems = 2, maxItems = 2 } } } } } }, required = new[] { "floorHeight", "floorCount", "useName", "footprintCurves" } }),

            BuildToolSchema("list_elements", "Lists all urban elements (transits, buildings, parcels, zones) in the current model.",
                new { type = "object", properties = new object { }, required = Array.Empty<string>() })
        };

        var response = new JsonObject
        {
            ["type"] = "ToolsList",
            ["requestId"] = requestId,
            ["tools"] = tools
        };

        Send(response.ToJsonString());
    }

    private void HandleToolCall(string? requestId, string? toolName, JsonElement? arguments)
    {
        if (string.IsNullOrEmpty(toolName))
        {
            SendError(requestId, "Tool name is required");
            return;
        }

        try
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc is null)
            {
                SendError(requestId, "No active Rhino document");
                return;
            }

            var model = UrbanModel.Instance;
            var tools = new UrbanModelTools(doc, model);

            JsonElement result;

            switch (toolName)
            {
                case "add_transit":
                    result = HandleAddTransit(tools, arguments);
                    break;

                case "add_building":
                    result = HandleAddBuilding(tools, arguments);
                    break;

                case "list_elements":
                    result = HandleListElements(model);
                    break;

                default:
                    SendError(requestId, $"Unknown tool: {toolName}");
                    return;
            }

            var response = new JsonObject
            {
                ["type"] = "ToolResult",
                ["requestId"] = requestId,
                ["content"] = new JsonArray { new JsonObject { ["type"] = "text", ["text"] = result.GetRawText() } }
            };

            Send(response.ToJsonString());
        }
        catch (Exception ex)
        {
            SendError(requestId, $"Tool execution error: {ex.Message}");
        }
    }

    private JsonElement HandleAddTransit(UrbanModelTools tools, JsonElement? args)
    {
        if (args is null)
            throw new ArgumentException("Arguments required");

        var width = args.Value.GetProperty("width").GetDouble();
        var typeName = args.Value.GetProperty("type").GetString() ?? "Road";
        var type = Enum.Parse<TransitType>(typeName);

        var curvesEl = args.Value.GetProperty("curveCoordinates");
        var curveCoordinates = new List<CurveCoordinates>();

        foreach (var item in curvesEl.EnumerateArray())
        {
            var coords = new List<(double X, double Y)>();
            foreach (var pt in item.GetProperty("coordinates").EnumerateArray())
            {
                var arr = pt.EnumerateArray().ToList();
                coords.Add((arr[0].GetDouble(), arr[1].GetDouble()));
            }
            curveCoordinates.Add(new CurveCoordinates(coords));
        }

        var metrics = tools.AddTransit(width, type, curveCoordinates);
        return JsonSerializer.SerializeToElement(metrics, JsonOptions);
    }

    private JsonElement HandleAddBuilding(UrbanModelTools tools, JsonElement? args)
    {
        if (args is null)
            throw new ArgumentException("Arguments required");

        var floorHeight = args.Value.GetProperty("floorHeight").GetDouble();
        var floorCount = args.Value.GetProperty("floorCount").GetInt32();
        var useName = args.Value.GetProperty("useName").GetString() ?? "Residential";
        var use = DefaultUses.Resolve(useName);

        var curvesEl = args.Value.GetProperty("footprintCurves");
        var footprints = new List<CurveCoordinates>();

        foreach (var item in curvesEl.EnumerateArray())
        {
            var coords = new List<(double X, double Y)>();
            foreach (var pt in item.GetProperty("coordinates").EnumerateArray())
            {
                var arr = pt.EnumerateArray().ToList();
                coords.Add((arr[0].GetDouble(), arr[1].GetDouble()));
            }
            footprints.Add(new CurveCoordinates(coords));
        }

        var metrics = tools.AddBuilding(floorHeight, floorCount, use, footprints);
        return JsonSerializer.SerializeToElement(metrics, JsonOptions);
    }

    private JsonElement HandleListElements(UrbanModel model)
    {
        var response = new ListElementsResponse(
            model.Transits.Select(t => new ElementSummary(t.GetGeometryIds()[0], "Transit", null, t.ToString(), null)).ToList(),
            model.Buildings.Select(b => new ElementSummary(b.GetGeometryIds()[0], "Building", b.FloorPlates.FirstOrDefault()?.Use?.Name, b.ToString(), null)).ToList(),
            model.Parcels.Select(p => new ElementSummary(p.GetGeometryIds()[0], "Parcel", p.Use?.Name, p.ToString(), null)).ToList(),
            model.Zones.Select(z => new ElementSummary(z.GetGeometryIds()[0], "Zone", z.Name, z.ToString(), null)).ToList());

        var json = ElementSummarySerializer.Serialize(response);
        return JsonDocument.Parse(json).RootElement;
    }

    private void SendError(string? requestId, string message)
    {
        var response = new JsonObject
        {
            ["type"] = "Error",
            ["requestId"] = requestId,
            ["error"] = message
        };
        Send(response.ToJsonString());
    }

    private static JsonNode BuildToolSchema(string name, string description, object inputSchema)
    {
        var schema = JsonSerializer.SerializeToNode(inputSchema, JsonOptions);
        return new JsonObject
        {
            ["name"] = name,
            ["description"] = description,
            ["inputSchema"] = schema
        };
    }
}
