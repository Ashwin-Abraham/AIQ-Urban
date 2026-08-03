using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.UrbanElements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIQ.Urban.Core.ModelActions;

public class ChangeBuildingUseStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<ChangeBuildingUseStep>();

    public StepResult Run(UrbanModel model, List<IModelEditData> creationData)
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

        _logger.LogInformation("Changed use of {Count} buildings to {Use}", touchedBuildings.Count, newUse.Name);

        var touchedIds = touchedBuildings.SelectMany(x => x.GetGeometryIds()).ToList();
        return new(touchedIds, touchedIds);
    }
}
