namespace AIQ.Urban.Core.ModelActions.EditData;

public record TransitRestoreData(Guid CenterlineId, Guid RejectedRhinoObjectId) : IModelEditData;
