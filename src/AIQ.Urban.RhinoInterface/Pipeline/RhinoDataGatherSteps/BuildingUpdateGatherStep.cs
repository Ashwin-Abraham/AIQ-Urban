using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.RhinoInterface.Pipeline.EditData;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class BuildingUpdateGatherStep(
    Guid oldEnvelopeId,
    Guid replacementRhinoObjectId,
    Brep newEnvelope,
    double typicalFloorHeight,
    List<Use> oldPlateUses) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc) =>
    [
        new BuildingUpdateData(oldEnvelopeId, newEnvelope, typicalFloorHeight, oldPlateUses),
        new RhinoObjectDeletionData(replacementRhinoObjectId)
    ];
}
