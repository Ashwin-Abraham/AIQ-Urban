using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.ModelActions;

public class UpdateBuildingStep : IUrbanModelStep
{
    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var data = creationData.OfType<BuildingUpdateData>().First();

        var oldBuilding = model.Buildings
            .FirstOrDefault(b => b.Envelope.Id == data.OldEnvelopeId);

        if (oldBuilding is null)
            return new([], []);

        var oldIds = oldBuilding.GetGeometryIds();
        model.Buildings.Remove(oldBuilding);

        var defaultUse = data.OldPlateUses.Count > 0
            ? data.OldPlateUses[0]
            : DefaultUses.Create("Public");
        var newBuilding = new Building(data.NewEnvelope, data.TypicalFloorHeight, defaultUse);

        MapUsesByZ(newBuilding.FloorPlates, data.OldPlateUses);

        model.AddBuilding(newBuilding);

        return new(newBuilding.GetGeometryIds(), oldIds);
    }

    private static void MapUsesByZ(IReadOnlyList<FloorPlate> newPlates, List<Use> oldUses)
    {
        if (oldUses.Count == 0) return;

        var newByZ = newPlates
            .Select((p, i) => (Plate: p, Index: i))
            .OrderBy(x => x.Plate.Z())
            .ToList();

        var oldByZ = oldUses
            .Select((u, i) => (Use: u, Index: i))
            .OrderBy(x => x.Index)
            .ToList();

        for (int i = 0; i < newByZ.Count; i++)
        {
            var oldIndex = (int)Math.Round((double)i * (oldByZ.Count - 1) / Math.Max(newByZ.Count - 1, 1));
            newByZ[i].Plate.SetUse(oldByZ[oldIndex].Use);
        }
    }
}
