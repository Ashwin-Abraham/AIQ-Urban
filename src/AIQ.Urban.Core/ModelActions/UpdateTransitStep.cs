using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.ModelActions;

public class UpdateTransitStep(Guid _oldCenterlineId, Curve _newCenterline) : IEditUrbanElementsStep
{
    public EditStepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var oldTransit = model.Transits
            .FirstOrDefault(t => t.GetGeometryIds().Contains(_oldCenterlineId));

        if (oldTransit is null)
            return new([],[]);

        var oldIds = oldTransit.GetGeometryIds();
        model.Transits.Remove(oldTransit);

        var newCenterlineId = Guid.NewGuid();
        var newCurve2D = new Curve2D(_newCenterline, newCenterlineId);

        var newTransit = new Transit(
            newCurve2D,
            oldTransit.Width,
            oldTransit.TransitType,
            oldTransit.IsOnGround);

        model.Transits.Add(newTransit);

        return new(newTransit.GetGeometryIds(), oldIds);
    }
}
