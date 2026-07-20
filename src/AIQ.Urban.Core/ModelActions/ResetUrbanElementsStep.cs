using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIQ.Urban.Core.ModelActions;

public class ResetUrbanElementsStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<ResetUrbanElementsStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var ids = model.GetUrbanElements()
            .SelectMany(e => e.GetGeometryIds())
            .Concat(model.TransitSurfaces.Select(ts => ts.Profile.Id))
            .ToList();

        var deletedIds = ids;
        model.Reset();

        _logger.LogInformation("Reset urban model — removed {Count} geometry objects", deletedIds.Count);

        return new([], deletedIds);
    }
}
