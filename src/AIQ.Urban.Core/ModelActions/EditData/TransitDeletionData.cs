namespace AIQ.Urban.Core.ModelActions.EditData;

public record TransitDeletionData(List<Guid> CenterlineIds) : IModelEditData;
