namespace AIQ.Urban.Core.ModelActions.CreationData;

public record TransitRestoreData(Guid CenterlineId, Guid RejectedRhinoObjectId) : ICreationData;
