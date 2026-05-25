using AIQ.Urban.Core.Models;

namespace AIQ.MCP.Models;

/// <summary>A group of buildings to change to a single use.
/// Produced by UrbanModelService.CreateChangeUseGroups and consumed by
/// PipelineConfigurations.WithBuildingUseChangeSteps.</summary>
public record BuildingUseChangeGroup(
    List<Guid> GeometryIds,
    Use NewUse);
