using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIQ.MCP.Models;

/// <summary>Request record for the change_building_use MCP tool. Accepts an array of change items.</summary>
public record ChangeBuildingUseRequest(List<ChangeUseItem> Items)
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
}

/// <summary>A single building use change.</summary>
public record ChangeUseItem(
    [property: JsonPropertyName("buildingGeometryId")] Guid BuildingGeometryId,
    [property: JsonPropertyName("newUseName")] string NewUseName);
