using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIQ.MCP.Models;

/// <summary>Request record for the add_transit MCP tool. Accepts an array of transit items.</summary>
public class AddTransitRequest(List<TransitItem> Items)
{
    /// <summary>JSON Schema describing the shape of this request for MCP tool registration.</summary>
    public static JsonElement JsonSchema { get; } = JsonSerializer.Deserialize<JsonElement>("""
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
}

/// <summary>A single transit corridor to create.</summary>
public record TransitItem(
    [property: JsonPropertyName("centerline")] double[][] CenterlineCoords,
    [property: JsonPropertyName("width")] double Width,
    [property: JsonPropertyName("transitType")] string TransitType);
