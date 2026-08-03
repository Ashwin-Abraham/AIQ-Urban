using AIQ.Urban.Core.ModelActions.EditData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIQ.Urban.Core.ModelActions;

public class AddTransitStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AddTransitStep>();

    public StepResult Run(UrbanModel model, List<IModelEditData> creationData)
    {
        var transits = creationData.OfType<TransitData>().Select(x => x.ToTransit()).ToList();
        model.AddTransit(transits);

        _logger.LogInformation("Added {Count} transits", transits.Count);

        return new(transits.SelectMany(x => x.GetGeometryIds()).ToList(), []);
    }
}
