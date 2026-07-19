namespace AIQ.Urban.Core.ModelActions.CreationData;

public record BuildingDeletionData(List<Guid> GeometryIds) : ICreationData;
