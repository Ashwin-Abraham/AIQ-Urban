using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.ModelActions;

public class UpdateZoneStep : IUrbanModelStep
{
    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var data = creationData.OfType<ZoneUpdateData>().First();

        var oldZone = model.Zones
            .FirstOrDefault(z => z.GetGeometryIds().Contains(data.OldBoundaryId));

        if (oldZone is null)
            return new([],[]);

        var oldId = oldZone.Boundary.Id;
        model.Zones.Remove(oldZone);

        var newBoundary = new Polygon2D(data.NewBoundary);
        var newZone = new Zone(newBoundary, oldZone.Name);
        model.Zones.Add(newZone);

        return new([newZone.Boundary.Id], [oldId]);
    }
}
