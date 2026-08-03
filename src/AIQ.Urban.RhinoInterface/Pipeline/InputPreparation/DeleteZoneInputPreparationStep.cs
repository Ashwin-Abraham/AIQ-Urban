using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;

public class DeleteZoneInputPreparationStep(IEnumerable<Guid> geometryIds) : IInputPreparationStep
{
    private readonly List<Guid> _geometryIds = geometryIds.Distinct().ToList();

    public List<ICreationData> Run(InputPreparationContext context)
    {
        var ids = _geometryIds
            .Select(x => UrbanModelInputResolver.FindByGeometryId<Zone>(context.UrbanModel, x))
            .Where(x => x is not null)
            .SelectMany(x => x!.GetGeometryIds())
            .Distinct()
            .ToList();
        return [new ZoneDeletionData(ids)];
    }
}
