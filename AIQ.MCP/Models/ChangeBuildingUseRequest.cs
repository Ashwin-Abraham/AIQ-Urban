namespace AIQ.MCP.Models;

/// <summary>Request record for the change_building_use MCP tool. Accepts an array of change items.</summary>
public record ChangeBuildingUseRequest(List<ChangeUseItem> Items);

/// <summary>A single building use change.</summary>
public record ChangeUseItem(
    Guid BuildingGeometryId,
    string NewUseName);
