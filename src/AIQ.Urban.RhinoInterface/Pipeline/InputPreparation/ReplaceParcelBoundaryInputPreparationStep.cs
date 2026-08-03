using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class ReplaceParcelBoundaryInputPreparationStep(
    Guid oldGeometryId,
    Guid replacementRhinoObjectId) : IInputPreparationStep
{
    public List<ICreationData> Run(InputPreparationContext context)
    {
        var boundary = UrbanModelInputResolver.FindByGeometryId<ParcelBoundary>(context.UrbanModel, oldGeometryId);
        if (boundary is null)
            return [];

        var replacement = context.RhinoDoc.Objects.FindId(replacementRhinoObjectId);
        if (replacement?.Geometry is not Curve curve || !curve.TryNormalizeParcelBoundary(out var normalized))
            return [new RhinoObjectRestorationData(replacementRhinoObjectId, boundary.GetGeometryIds())];

        return
        [
            new ParcelBoundaryUpdateData(boundary.Id, new Curve2D(normalized, replacementRhinoObjectId)),
            new RhinoObjectDeletionData(replacementRhinoObjectId),
        ];
    }
}
