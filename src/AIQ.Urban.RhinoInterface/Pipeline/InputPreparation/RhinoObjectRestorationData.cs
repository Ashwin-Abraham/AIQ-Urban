using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public record RhinoObjectRestorationData(
    Guid RejectedRhinoObjectId,
    List<Guid> GeometryIdsToRestore) : ICreationData;
