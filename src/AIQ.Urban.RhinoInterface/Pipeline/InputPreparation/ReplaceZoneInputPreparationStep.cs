using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class ReplaceZoneInputPreparationStep(
    Guid oldGeometryId,
    Guid replacementRhinoObjectId) : IInputPreparationStep
{
    public List<ICreationData> Run(InputPreparationContext context)
    {
        var zone = UrbanModelInputResolver.FindByGeometryId<Zone>(context.UrbanModel, oldGeometryId);
        if (zone is null)
            return [];

        var replacement = context.RhinoDoc.Objects.FindId(replacementRhinoObjectId);
        if (replacement?.Geometry is not Curve curve || !curve.IsClosed || !curve.IsPlanar())
            return [new RhinoObjectRestorationData(replacementRhinoObjectId, zone.GetGeometryIds())];

        var normalized = curve;
        if (curve.TryGetPlane(out var plane) && plane.Normal.Z < 0)
        {
            normalized = curve.DuplicateCurve();
            normalized.Reverse();
        }

        return
        [
            new ZoneUpdateData(zone.Boundary.Id, normalized),
            new RhinoObjectDeletionData(replacementRhinoObjectId),
        ];
    }
}
