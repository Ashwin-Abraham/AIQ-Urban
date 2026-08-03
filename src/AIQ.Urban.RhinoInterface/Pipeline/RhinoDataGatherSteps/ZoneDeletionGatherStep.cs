using AIQ.Urban.Core.ModelActions.EditData;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class ZoneDeletionGatherStep(List<Guid> geometryIds) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc) => [new ZoneDeletionData(geometryIds)];
}
