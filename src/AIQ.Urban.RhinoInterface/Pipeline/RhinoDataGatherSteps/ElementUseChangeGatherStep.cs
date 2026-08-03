using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class ElementUseChangeGatherStep(List<Guid> geometryIds, Use newUse) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc) =>
    [
        new ParcelUseChangeData(geometryIds, newUse),
        new BuildingUseChangeData(geometryIds, newUse)
    ];
}
