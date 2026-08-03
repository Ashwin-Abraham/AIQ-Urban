using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class DeleteParcelBoundaryInputPreparationStep(IEnumerable<Guid> geometryIds) : IInputPreparationStep
{
    private readonly List<Guid> _geometryIds = geometryIds.Distinct().ToList();

    public List<ICreationData> Run(InputPreparationContext context)
    {
        var ids = _geometryIds
            .Select(x => UrbanModelInputResolver.FindByGeometryId<ParcelBoundary>(context.UrbanModel, x))
            .Where(x => x is not null)
            .Select(x => x!.Id)
            .Distinct()
            .ToList();
        return [new ParcelBoundaryDeletionData(ids)];
    }
}
