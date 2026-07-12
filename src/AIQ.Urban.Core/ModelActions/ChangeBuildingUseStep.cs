using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions;

public class ChangeBuildingUseStep(
    List<Guid> geometryIds,
    Use newUse) : IEditUrbanElementsStep
{
    public EditStepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var touchedBuildings = new HashSet<Building>();

        // Pass 1: Process envelope matches (building-wide)
        foreach (var building in model.Buildings)
        {
            if (!geometryIds.Contains(building.Envelope.Id))
                continue;

            touchedBuildings.Add(building);

            foreach (var plate in building.FloorPlates)
                plate.SetUse(newUse);
        }

        // Pass 2: Process floor plate outline matches (per-plate)
        foreach (var building in model.Buildings)
        {
            // Skip buildings already touched in pass 1 (building-wins priority)
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

        var newIds = touchedBuildings.SelectMany(x => x.GetGeometryIds()).ToList();
        var oldIds = touchedBuildings.SelectMany(x => x.GetGeometryIds()).ToList();
        return new(newIds, oldIds);
    }
}
