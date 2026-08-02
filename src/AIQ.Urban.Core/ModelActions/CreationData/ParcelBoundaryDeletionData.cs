namespace AIQ.Urban.Core.ModelActions.CreationData;

public record ParcelBoundaryDeletionData(List<Guid> BoundaryIds) : ICreationData;
