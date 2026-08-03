using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.RhinoInterface.Pipeline.EditData;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class TransitUpdateGatherStep(Guid oldCenterlineId, Guid newCenterlineId, Curve newCenterline) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc) =>
    [
        new TransitUpdateData(oldCenterlineId, newCenterline),
        new RhinoObjectDeletionData(newCenterlineId)
    ];
}
