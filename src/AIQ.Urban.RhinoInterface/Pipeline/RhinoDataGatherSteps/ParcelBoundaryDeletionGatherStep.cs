using AIQ.Urban.Core.ModelActions.EditData;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class ParcelBoundaryDeletionGatherStep(List<Guid> boundaryIds) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc) => [new ParcelBoundaryDeletionData(boundaryIds)];
}
