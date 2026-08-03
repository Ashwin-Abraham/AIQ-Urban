using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.Pipeline.EditData;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;

public class FloorPlateReplaceGatherStep(FloorPlate oldPlate, Guid replacementRhinoObjectId, double newZ) : IRhinoDataGatherStep
{
    public List<IModelEditData> Run(RhinoDoc doc) =>
    [
        new FloorPlateReplaceData(oldPlate, newZ),
        new RhinoObjectDeletionData(replacementRhinoObjectId)
    ];
}
