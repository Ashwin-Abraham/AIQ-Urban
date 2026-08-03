using AIQ.Urban.Core.ModelActions.EditData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIQ.Urban.Core.ModelActions;

public class RemoveBuildingStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<RemoveBuildingStep>();

    public StepResult Run(UrbanModel model, List<IModelEditData> creationData)
    {
        var geometryIds = creationData.OfType<BuildingDeletionData>()
            .SelectMany(d => d.GeometryIds)
            .ToList();

        var buildingsToRemove = model.Buildings
            .Where(x => x.GetGeometryIds()
                .Any(id => geometryIds.Contains(id)))
            .ToList();

        model.Buildings.RemoveAll(x => buildingsToRemove.Contains(x));

        _logger.LogInformation("Removed {Count} buildings", buildingsToRemove.Count);

        return new([], buildingsToRemove.SelectMany(x => x.GetGeometryIds()).ToList());
    }
}
