using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class BuildingUseChangeGatherStep(List<Guid> geometryIds, Use newUse) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc) => [new BuildingUseChangeData(geometryIds, newUse)];
}
