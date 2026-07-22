using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIQ.Urban.Core.ModelActions;

public class AddParcelBoundaryStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AddParcelBoundaryStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var boundaries = creationData.OfType<ParcelBoundaryData>()
            .Select(x => x.ToParcelBoundary())
            .ToList();

        foreach (var boundary in boundaries)
        {
            model.AddParcelBoundary(boundary);
        }

        _logger.LogInformation("Added {Count} parcel boundaries", boundaries.Count);

        return new(boundaries.SelectMany(x => x.GetGeometryIds()).ToList(), []);
    }
}
