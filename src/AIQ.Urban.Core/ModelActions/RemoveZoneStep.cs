using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIQ.Urban.Core.ModelActions;

public class RemoveZoneStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<RemoveZoneStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var geometryIds = creationData.OfType<ZoneDeletionData>()
            .SelectMany(d => d.GeometryIds)
            .Distinct()
            .ToList();

        var zonesToRemove = model.Zones
            .Where(x => x.GetGeometryIds()
                .Any(id => geometryIds.Contains(id)))
            .ToList();

        model.Zones.RemoveAll(x => zonesToRemove.Contains(x));

        _logger.LogInformation("Removed {Count} zones", zonesToRemove.Count);

        return new([], zonesToRemove.SelectMany(x => x.GetGeometryIds()).ToList());
    }
}
