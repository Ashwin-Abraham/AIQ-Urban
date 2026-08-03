using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class ReplaceFloorPlateInputPreparationStep(
    Guid oldGeometryId,
    Guid replacementRhinoObjectId) : IInputPreparationStep
{
    public List<ICreationData> Run(InputPreparationContext context)
    {
        var plate = UrbanModelInputResolver.FindByGeometryId<FloorPlate>(context.UrbanModel, oldGeometryId);
        if (plate is null)
            return [];

        var replacement = context.RhinoDoc.Objects.FindId(replacementRhinoObjectId);
        if (replacement?.Geometry is not Curve curve)
            return [new RhinoObjectRestorationData(replacementRhinoObjectId, plate.GetGeometryIds())];

        return
        [
            new FloorPlateReplaceData(plate, curve.PointAtStart.Z),
            new RhinoObjectDeletionData(replacementRhinoObjectId),
        ];
    }
}
