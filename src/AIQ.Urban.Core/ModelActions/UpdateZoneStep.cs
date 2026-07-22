using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino.Geometry;

namespace AIQ.Urban.Core.ModelActions;

public class UpdateZoneStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<UpdateZoneStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var data = creationData.OfType<ZoneUpdateData>().First();

        var oldZone = model.Zones
            .FirstOrDefault(z => z.GetGeometryIds().Contains(data.OldBoundaryId));

        if (oldZone is null)
            return new([],[]);

        var oldId = oldZone.Boundary.Id;
        model.Zones.Remove(oldZone);

        var newBoundary = new Polycurve2D(data.NewBoundary);
        var newZone = new Zone(newBoundary, oldZone.Name);
        model.Zones.Add(newZone);

        _logger.LogInformation("Updated zone {Id}", data.OldBoundaryId);

        return new([newZone.Boundary.Id], [oldId]);
    }
}
