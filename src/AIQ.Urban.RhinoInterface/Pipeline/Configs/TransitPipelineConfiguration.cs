using AIQ.Urban.Core.ModelActions;

namespace AIQ.Urban.RhinoInterface.Pipeline.Configs;

public record TransitPipelineConfiguration(
    List<TransitData> TransitData,
    List<Guid> CenterlineIds);
