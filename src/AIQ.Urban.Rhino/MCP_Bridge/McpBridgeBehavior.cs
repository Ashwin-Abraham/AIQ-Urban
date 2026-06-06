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

namespace AIQ.Urban.RhinoPlugin.MCP_Bridge;

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
                new { type = "object", properties = new object { }, required = Array.Empty<string>() }),

            BuildToolSchema("add_zone", "Adds a zone boundary with a name.",
                new { type = "object", properties = new { name = new { type = "string", description = "Zone name" }, boundaryCoordinates = new { type = "array", description = "Boundary XY coordinates (closed ring)", items = new { type = "object", properties = new { coordinates = new { type = "array", items = new { type = "array", items = new { type = "number" }, minItems = 2, maxItems = 2 } } } } } }, required = new[] { "name", "boundaryCoordinates" } }),

            BuildToolSchema("delete_transit", "Deletes transits by their centerline IDs.",
                new { type = "object", properties = new { centerlineIds = new { type = "array", description = "Centerline geometry IDs to delete", items = new { type = "string" } } }, required = new[] { "centerlineIds" } }),

            BuildToolSchema("delete_building", "Deletes buildings by their geometry IDs.",
                new { type = "object", properties = new { geometryIds = new { type = "array", description = "Building geometry IDs to delete", items = new { type = "string" } } }, required = new[] { "geometryIds" } }),

            BuildToolSchema("delete_zone", "Deletes zones by their boundary geometry IDs.",
                new { type = "object", properties = new { geometryIds = new { type = "array", description = "Zone boundary geometry IDs to delete", items = new { type = "string" } } }, required = new[] { "geometryIds" } }),

            BuildToolSchema("change_building_use", "Changes the use of buildings or individual floor plates.",
                new { type = "object", properties = new { geometryIds = new { type = "array", description = "Envelope or floor plate outline IDs", items = new { type = "string" } }, useName = new { type = "string", description = "New use type (e.g. Residential, Commercial)" } }, required = new[] { "geometryIds", "useName" } }),

            BuildToolSchema("compute_metrics", "Recomputes project metrics (FAR, coverage, area per use, transit lengths, zone metrics) from current model state.",
                new { type = "object", properties = new { tolerance = new { type = "number", description = "Containment tolerance (default 0.001)" } }, required = Array.Empty<string>() }),

            BuildToolSchema("list_available_uses", "Returns all available use types that can be assigned to buildings or floor plates.",
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

                case "add_zone":
                    result = HandleAddZone(tools, arguments);
                    break;

                case "delete_transit":
                    result = HandleDeleteTransit(tools, arguments);
                    break;

                case "delete_building":
                    result = HandleDeleteBuilding(tools, arguments);
                    break;

                case "delete_zone":
                    result = HandleDeleteZone(tools, arguments);
                    break;

                case "change_building_use":
                    result = HandleChangeBuildingUse(tools, arguments);
                    break;

                case "compute_metrics":
                    result = HandleComputeMetrics(tools, arguments);
                    break;

                case "list_available_uses":
                    result = HandleListAvailableUses();
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

        var result = tools.AddBuilding(floorHeight, floorCount, use, footprints);
        return JsonSerializer.SerializeToElement(result, JsonOptions);
    }

    private JsonElement HandleAddZone(UrbanModelTools tools, JsonElement? args)
    {
        if (args is null)
            throw new ArgumentException("Arguments required");

        var name = args.Value.GetProperty("name").GetString() ?? "";
        var boundaryEl = args.Value.GetProperty("boundaryCoordinates");
        var boundaryCoordinates = new List<CurveCoordinates>();

        foreach (var item in boundaryEl.EnumerateArray())
        {
            var coords = new List<(double X, double Y)>();
            foreach (var pt in item.GetProperty("coordinates").EnumerateArray())
            {
                var arr = pt.EnumerateArray().ToList();
                coords.Add((arr[0].GetDouble(), arr[1].GetDouble()));
            }
            boundaryCoordinates.Add(new CurveCoordinates(coords));
        }

        var zoneResult = tools.AddZone(name, boundaryCoordinates);
        return JsonSerializer.SerializeToElement(zoneResult, JsonOptions);
    }

    private JsonElement HandleDeleteTransit(UrbanModelTools tools, JsonElement? args)
    {
        if (args is null)
            throw new ArgumentException("Arguments required");

        var ids = args.Value.GetProperty("centerlineIds")
            .EnumerateArray()
            .Select(x => Guid.Parse(x.GetString()!))
            .ToList();

        var result = tools.DeleteTransit(ids);
        return JsonSerializer.SerializeToElement(result, JsonOptions);
    }

    private JsonElement HandleDeleteBuilding(UrbanModelTools tools, JsonElement? args)
    {
        if (args is null)
            throw new ArgumentException("Arguments required");

        var ids = args.Value.GetProperty("geometryIds")
            .EnumerateArray()
            .Select(x => Guid.Parse(x.GetString()!))
            .ToList();

        var result = tools.DeleteBuilding(ids);
        return JsonSerializer.SerializeToElement(result, JsonOptions);
    }

    private JsonElement HandleDeleteZone(UrbanModelTools tools, JsonElement? args)
    {
        if (args is null)
            throw new ArgumentException("Arguments required");

        var ids = args.Value.GetProperty("geometryIds")
            .EnumerateArray()
            .Select(x => Guid.Parse(x.GetString()!))
            .ToList();

        var result = tools.DeleteZone(ids);
        return JsonSerializer.SerializeToElement(result, JsonOptions);
    }

    private JsonElement HandleChangeBuildingUse(UrbanModelTools tools, JsonElement? args)
    {
        if (args is null)
            throw new ArgumentException("Arguments required");

        var ids = args.Value.GetProperty("geometryIds")
            .EnumerateArray()
            .Select(x => Guid.Parse(x.GetString()!))
            .ToList();
        var useName = args.Value.GetProperty("useName").GetString() ?? "Residential";

        var result = tools.ChangeBuildingUse(ids, useName);
        return JsonSerializer.SerializeToElement(result, JsonOptions);
    }

    private JsonElement HandleComputeMetrics(UrbanModelTools tools, JsonElement? args)
    {
        var tolerance = 0.001;
        if (args is not null && args.Value.TryGetProperty("tolerance", out var tol))
            tolerance = tol.GetDouble();

        var result = tools.ComputeMetrics(tolerance);
        return JsonDocument.Parse(result).RootElement;
    }

    private static JsonElement HandleListAvailableUses()
    {
        return JsonSerializer.SerializeToElement(DefaultUses.Names, JsonOptions);
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
