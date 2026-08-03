namespace AIQ.Urban.Core.ModelActions.EditData;

public record ZoneDeletionData(List<Guid> GeometryIds) : IModelEditData;
