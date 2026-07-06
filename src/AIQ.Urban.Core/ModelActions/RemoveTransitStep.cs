using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class RemoveTransitStep(List<Guid> _centerlineIds) : IDeleteUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model, List<ICreationData> creationData)
    {
        var transitsToRemove = model.Transits
            .Where(x => x.GetGeometryIds()
                .Any(id => _centerlineIds.Contains(id)))
            .ToList();

        model.Transits.RemoveAll(x => transitsToRemove.Contains(x));

        return transitsToRemove.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}
