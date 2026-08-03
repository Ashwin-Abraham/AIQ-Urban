namespace AIQ.Urban.Core.ModelActions.EditData;

public record BuildingDeletionData(List<Guid> GeometryIds) : IModelEditData;
