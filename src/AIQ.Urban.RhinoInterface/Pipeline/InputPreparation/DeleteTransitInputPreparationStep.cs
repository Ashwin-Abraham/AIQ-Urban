using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class DeleteTransitInputPreparationStep(IEnumerable<Guid> geometryIds) : IInputPreparationStep
{
    private readonly List<Guid> _geometryIds = geometryIds.Distinct().ToList();

    public List<ICreationData> Run(InputPreparationContext context)
    {
        var centerlineIds = _geometryIds
            .Select(x => UrbanModelInputResolver.FindByGeometryId<Transit>(context.UrbanModel, x))
            .Where(x => x is not null)
            .Select(x => x!.Centerline.Id)
            .Distinct()
            .ToList();
        return [new TransitDeletionData(centerlineIds)];
    }
}
