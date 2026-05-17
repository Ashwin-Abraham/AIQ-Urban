using System;
using System.Collections.Generic;
using AIQ.Urban.Core.ModelActions;

namespace AIQ.Urban.RhinoInterface.Pipeline.Configs;

public record BuildingPipelineConfiguration(
    List<BuildingData> BuildingData,
    List<Guid> InputCurveIds);
