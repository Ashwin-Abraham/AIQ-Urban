using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class AddTransitStep : IAddUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model, ICreationData? creationData)
    {
        var collection = creationData as CreationDataCollection<TransitData>
            ?? throw new InvalidOperationException("Expected CreationDataCollection<TransitData>.");

        var transits = collection.Items.Select(x => x.ToTransit()).ToList();
        model.AddTransit(transits);

        return transits.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}
