using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class AddTransitStep : IUrbanModelStep
{
    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var transits = creationData.OfType<TransitData>().Select(x => x.ToTransit()).ToList();
        model.AddTransit(transits);

        return new(transits.SelectMany(x => x.GetGeometryIds()).ToList(), []);
    }
}
