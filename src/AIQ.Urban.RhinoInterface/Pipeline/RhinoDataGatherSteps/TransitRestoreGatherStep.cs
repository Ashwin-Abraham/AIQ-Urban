using AIQ.Urban.Core.ModelActions.EditData;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class TransitRestoreGatherStep(Guid centerlineId, Guid rejectedRhinoObjectId) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc) => [new TransitRestoreData(centerlineId, rejectedRhinoObjectId)];
}
