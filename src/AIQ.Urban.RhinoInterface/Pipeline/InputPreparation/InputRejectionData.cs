using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public record InputRejectionData(
    string Source,
    int InputIndex,
    string Reason) : ICreationData;
