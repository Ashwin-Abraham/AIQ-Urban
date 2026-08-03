using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public record RhinoObjectDeletionData(Guid Id) : ICreationData;
