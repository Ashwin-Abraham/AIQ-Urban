using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIQ.Urban.Core.ModelActions;

public class RemoveTransitStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<RemoveTransitStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var centerlineIds = creationData.OfType<TransitDeletionData>()
            .SelectMany(d => d.CenterlineIds)
            .ToList();

        var transitsToRemove = model.Transits
            .Where(x => x.GetGeometryIds()
                .Any(id => centerlineIds.Contains(id)))
            .ToList();

        model.Transits.RemoveAll(x => transitsToRemove.Contains(x));

        _logger.LogInformation("Removed {Count} transits", transitsToRemove.Count);

        return new([], transitsToRemove.SelectMany(x => x.GetGeometryIds()).ToList());
    }
}
