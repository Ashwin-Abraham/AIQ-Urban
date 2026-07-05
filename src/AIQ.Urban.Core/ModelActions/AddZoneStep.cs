using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class AddZoneStep : IAddUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model, ICreationData? creationData)
    {
        var collection = creationData as CreationDataCollection<ZoneData>
            ?? throw new InvalidOperationException("Expected CreationDataCollection<ZoneData>.");

        var zones = collection.Items.Select(x => x.ToZone()).ToList();
        foreach (var zone in zones)
            model.AddZone(zone);

        return zones.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}
