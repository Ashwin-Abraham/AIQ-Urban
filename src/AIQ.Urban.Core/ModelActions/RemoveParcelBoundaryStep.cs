using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIQ.Urban.Core.ModelActions;

public class RemoveParcelBoundaryStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<RemoveParcelBoundaryStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var boundaryIds = creationData.OfType<ParcelBoundaryDeletionData>()
            .SelectMany(x => x.BoundaryIds)
            .Distinct()
            .ToHashSet();
        var boundaries = model.ParcelBoundaries
            .Where(x => boundaryIds.Contains(x.Id))
            .ToList();

        model.ParcelBoundaries.RemoveAll(boundaries.Contains);

        _logger.LogInformation("Removed {Count} parcel boundaries", boundaries.Count);
        return new([], boundaries.Select(x => x.Id).ToList());
    }
}
