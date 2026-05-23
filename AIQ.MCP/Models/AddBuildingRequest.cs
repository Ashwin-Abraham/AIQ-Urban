namespace AIQ.MCP.Models;

/// <summary>Request record for the add_building MCP tool. Accepts an array of building items.</summary>
public record AddBuildingRequest(List<BuildingItem> Items);

/// <summary>A single building to create.</summary>
public record BuildingItem(
    double[][] FootprintCoords,
    double TypicalFloorHeight,
    int FloorCount,
    string UseName);
