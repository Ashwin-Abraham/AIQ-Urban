namespace AIQ.Urban.Core.ModelActions.EditData;

public record ParcelBoundaryDeletionData(List<Guid> BoundaryIds) : IModelEditData;
