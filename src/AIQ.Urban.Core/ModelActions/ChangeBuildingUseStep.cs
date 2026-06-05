using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions;

public class ChangeBuildingUseStep(
    List<Guid> geometryIds,
    Use newUse) : IEditUrbanElementsStep
{
    public EditStepResult Run(UrbanModel model)
    {
        var oldIds = new List<Guid>();
        var newIds = new List<Guid>();
        var touchedBuildings = new HashSet<Building>();

        // Pass 1: Process envelope matches (building-wide)
        foreach (var building in model.Buildings)
        {
            if (!geometryIds.Contains(building.Envelope.Id))
                continue;

            touchedBuildings.Add(building);
            oldIds.Add(building.Envelope.Id);
            oldIds.AddRange(building.FloorPlates.Select(p => p.Outline.Id));

            foreach (var plate in building.FloorPlates)
                plate.SetUse(newUse);

            newIds.Add(building.Envelope.Id);
            newIds.AddRange(building.FloorPlates.Select(p => p.Outline.Id));
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

                oldIds.Add(plate.Outline.Id);
                plate.SetUse(newUse);
                newIds.Add(plate.Outline.Id);
                touchedBuildings.Add(building);
            }
        }

        return new(newIds, oldIds);
    }
}
