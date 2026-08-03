namespace AIQ.Urban.Core.ModelActions.CreationData;

public record ZoneDeletionData(List<Guid> GeometryIds) : ICreationData;
