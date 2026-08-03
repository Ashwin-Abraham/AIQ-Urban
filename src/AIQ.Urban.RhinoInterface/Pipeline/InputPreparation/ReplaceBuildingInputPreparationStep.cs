using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class ReplaceBuildingInputPreparationStep(
    Guid oldGeometryId,
    Guid replacementRhinoObjectId) : IInputPreparationStep
{
    public List<ICreationData> Run(InputPreparationContext context)
    {
        var building = UrbanModelInputResolver.FindByGeometryId<Building>(context.UrbanModel, oldGeometryId);
        if (building is null)
            return [];

        var replacement = context.RhinoDoc.Objects.FindId(replacementRhinoObjectId);
        var brep = replacement?.Geometry switch
        {
            Brep value => value,
            Extrusion extrusion => extrusion.ToBrep(),
            _ => null,
        };

        if (brep is null)
            return [new RhinoObjectRestorationData(replacementRhinoObjectId, building.GetGeometryIds())];

        var oldPlateUses = building.FloorPlates
            .OrderBy(x => x.Z())
            .Select(x => x.Use)
            .ToList();

        return
        [
            new BuildingUpdateData(building.Envelope.Id, brep, building.TypicalFloorHeight, oldPlateUses),
            new RhinoObjectDeletionData(replacementRhinoObjectId),
        ];
    }
}
