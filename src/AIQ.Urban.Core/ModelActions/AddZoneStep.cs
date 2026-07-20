using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIQ.Urban.Core.ModelActions;

public class AddZoneStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AddZoneStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var zones = creationData.OfType<ZoneData>().Select(x => x.ToZone()).ToList();
        foreach (var zone in zones)
            model.AddZone(zone);

        _logger.LogInformation("Added {Count} zones", zones.Count);

        return new(zones.SelectMany(x => x.GetGeometryIds()).ToList(), []);
    }
}
