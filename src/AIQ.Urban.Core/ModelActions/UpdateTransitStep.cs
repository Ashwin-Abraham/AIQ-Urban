using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.ModelActions;

public class UpdateTransitStep : IUrbanModelStep
{
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
            oldTransit.TransitType,
            oldTransit.IsOnGround);

        model.Transits.Add(newTransit);

        return new(newTransit.GetGeometryIds(), oldIds);
    }
}
