using AIQ.Urban.Core.ModelActions.EditData;

namespace AIQ.Urban.RhinoInterface.Pipeline.EditData;

public record RhinoObjectDeletionData(Guid Id) : IModelEditData;
