using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.RhinoInterface.Pipeline.EditData;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class ParcelBoundaryUpdateGatherStep(Guid oldBoundaryId, Guid replacementRhinoObjectId, Curve newBoundary) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc) =>
    [
        new ParcelBoundaryUpdateData(oldBoundaryId, new Curve2D(newBoundary, replacementRhinoObjectId)),
        new RhinoObjectDeletionData(replacementRhinoObjectId)
    ];
}
