using AIQ.Urban.Core.ModelActions;

namespace AIQ.Urban.RhinoInterface.Pipeline.Configs;

public record ZonePipelineConfiguration(
    List<ZoneData> ZoneData,
    List<Guid> InputCurveIds);
