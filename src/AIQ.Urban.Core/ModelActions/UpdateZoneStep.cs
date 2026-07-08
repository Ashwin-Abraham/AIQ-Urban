using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.ModelActions;

public class UpdateZoneStep(Guid _oldBoundaryId, Curve _newBoundary) : IEditUrbanElementsStep
{
    public EditStepResult Run(UrbanModel model)
    {
        var oldZone = model.Zones
            .FirstOrDefault(z => z.GetGeometryIds().Contains(_oldBoundaryId));

        if (oldZone is null)
            return new([],[]);

        var oldId = oldZone.Boundary.Id;
        model.Zones.Remove(oldZone);

        var newBoundary = new Polygon2D(_newBoundary);
        var newZone = new Zone(newBoundary, oldZone.Name);
        model.Zones.Add(newZone);

        return new([newZone.Boundary.Id], [oldId]);
    }
}
