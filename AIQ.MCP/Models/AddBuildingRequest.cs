using System.Text.Json.Serialization;

namespace AIQ.MCP.Models;

/// <summary>Request record for the add_building MCP tool. Accepts an array of building items.</summary>
public record AddBuildingRequest(List<BuildingItem> Items);

/// <summary>A single building to create.</summary>
public record BuildingItem(
    [property: JsonPropertyName("footprint")] double[][] FootprintCoords,
    [property: JsonPropertyName("typicalFloorHeight")] double TypicalFloorHeight,
    [property: JsonPropertyName("floorCount")] int FloorCount,
    [property: JsonPropertyName("useName")] string UseName);
