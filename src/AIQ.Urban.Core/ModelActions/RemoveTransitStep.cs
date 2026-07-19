using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class RemoveTransitStep : IUrbanModelStep
{
    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var centerlineIds = creationData.OfType<TransitDeletionData>()
            .SelectMany(d => d.CenterlineIds)
            .ToList();

        var transitsToRemove = model.Transits
            .Where(x => x.GetGeometryIds()
                .Any(id => centerlineIds.Contains(id)))
            .ToList();

        model.Transits.RemoveAll(x => transitsToRemove.Contains(x));

        return new([], transitsToRemove.SelectMany(x => x.GetGeometryIds()).ToList());
    }
}
