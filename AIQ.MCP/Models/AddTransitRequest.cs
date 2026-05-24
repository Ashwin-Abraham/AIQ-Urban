using System.Text.Json.Serialization;

namespace AIQ.MCP.Models;

/// <summary>Request record for the add_transit MCP tool. Accepts an array of transit items.</summary>
public record AddTransitRequest(List<TransitItem> Items);

/// <summary>A single transit corridor to create.</summary>
public record TransitItem(
    [property: JsonPropertyName("centerline")] double[][] CenterlineCoords,
    [property: JsonPropertyName("width")] double Width,
    [property: JsonPropertyName("transitType")] string TransitType);
