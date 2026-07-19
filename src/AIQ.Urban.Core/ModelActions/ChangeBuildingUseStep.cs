using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions;

public class ChangeBuildingUseStep : IUrbanModelStep
{
    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var data = creationData.OfType<BuildingUseChangeData>().First();
        var geometryIds = data.GeometryIds;
        var newUse = data.NewUse;

        var touchedBuildings = new HashSet<Building>();

        foreach (var building in model.Buildings)
        {
            if (!geometryIds.Contains(building.Envelope.Id))
                continue;

            touchedBuildings.Add(building);

            foreach (var plate in building.FloorPlates)
                plate.SetUse(newUse);
        }

        foreach (var building in model.Buildings)
        {
            if (touchedBuildings.Contains(building))
                continue;

            foreach (var plate in building.FloorPlates)
            {
                if (!geometryIds.Contains(plate.Outline.Id))
                    continue;

                plate.SetUse(newUse);
                touchedBuildings.Add(building);
            }
        }

        var touchedIds = touchedBuildings.SelectMany(x => x.GetGeometryIds()).ToList();
        return new(touchedIds, touchedIds);
    }
}
