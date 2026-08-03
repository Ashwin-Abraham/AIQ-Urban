using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.UrbanElements;
using Microsoft.Extensions.Logging;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class ReplaceTransitInputPreparationStep(
    Guid oldGeometryId,
    Guid replacementRhinoObjectId) : IInputPreparationStep
{
    public List<ICreationData> Run(InputPreparationContext context)
    {
        var transit = UrbanModelInputResolver.FindByGeometryId<Transit>(context.UrbanModel, oldGeometryId);
        if (transit is null)
            return [];

        var replacement = context.RhinoDoc.Objects.FindId(replacementRhinoObjectId);
        if (replacement?.Geometry is Curve curve && curve.TryNormalizeTransitCenterline(out var normalized))
        {
            return
            [
                new TransitUpdateData(transit.Centerline.Id, normalized),
                new RhinoObjectDeletionData(replacementRhinoObjectId),
            ];
        }

        context.LoggerFactory.CreateLogger<ReplaceTransitInputPreparationStep>()
            .LogWarning("Rejected replacement for transit centerline {CenterlineId}: invalid Rhino object {ObjectId}", transit.Centerline.Id, replacementRhinoObjectId);

        return [new RhinoObjectRestorationData(replacementRhinoObjectId, transit.GetGeometryIds())];
    }
}
