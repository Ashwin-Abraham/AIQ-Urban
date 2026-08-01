using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino.Geometry;

namespace AIQ.Urban.Core.ModelActions;

public class UpdateTransitStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<UpdateTransitStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var data = creationData.OfType<TransitUpdateData>().First();

        var oldTransit = model.Transits
            .FirstOrDefault(t => t.GetGeometryIds().Contains(data.OldCenterlineId));

        if (oldTransit is null)
            return new([],[]);

        var oldIds = oldTransit.GetGeometryIds();
        model.Transits.Remove(oldTransit);

        var newCenterlineId = Guid.NewGuid();
        var newCurve2D = new Curve2D(data.NewCenterline, newCenterlineId);

        var newTransit = new Transit(
            newCurve2D,
            oldTransit.Width,
            oldTransit.TransitType);

        model.Transits.Add(newTransit);

        _logger.LogInformation("Updated transit {Id}", data.OldCenterlineId);

        return new(newTransit.GetGeometryIds(), oldIds);
    }
}
