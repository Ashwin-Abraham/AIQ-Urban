using AIQ.Urban.Core.ModelActions.EditData;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class ZoneUpdateGatherStep(Guid oldBoundaryId, Curve newBoundary) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc) => [new ZoneUpdateData(oldBoundaryId, newBoundary)];
}
