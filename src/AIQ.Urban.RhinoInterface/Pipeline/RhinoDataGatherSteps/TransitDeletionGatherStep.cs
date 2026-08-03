using AIQ.Urban.Core.ModelActions.EditData;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class TransitDeletionGatherStep(List<Guid> centerlineIds) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc) => [new TransitDeletionData(centerlineIds)];
}
