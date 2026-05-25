using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIQ.MCP.Models;

/// <summary>Request record for the add_building MCP tool. Accepts an array of building items.</summary>
public record AddBuildingRequest(List<BuildingItem> Items)
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
}

/// <summary>A single building to create.</summary>
public record BuildingItem(
    [property: JsonPropertyName("footprint")] double[][] FootprintCoords,
    [property: JsonPropertyName("typicalFloorHeight")] double TypicalFloorHeight,
    [property: JsonPropertyName("floorCount")] int FloorCount,
    [property: JsonPropertyName("useName")] string UseName);
