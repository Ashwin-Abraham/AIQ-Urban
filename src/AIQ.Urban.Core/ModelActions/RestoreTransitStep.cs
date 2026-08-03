using AIQ.Urban.Core.ModelActions.EditData;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIQ.Urban.Core.ModelActions;

public class RestoreTransitStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<RestoreTransitStep>();

    public StepResult Run(UrbanModel model, List<IModelEditData> creationData)
    {
        var data = creationData.OfType<TransitRestoreData>().First();
        var transit = model.Transits.FirstOrDefault(x => x.Centerline.Id == data.CenterlineId);
        if (transit is null)
            return new([], []);

        _logger.LogInformation("Restoring transit {Id} after a rejected centerline replacement", data.CenterlineId);
        return new(transit.GetGeometryIds(), [data.RejectedRhinoObjectId]);
    }
}
