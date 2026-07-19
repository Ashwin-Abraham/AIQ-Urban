using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class AddZoneStep : IUrbanModelStep
{
    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var zones = creationData.OfType<ZoneData>().Select(x => x.ToZone()).ToList();
        foreach (var zone in zones)
            model.AddZone(zone);

        return new(zones.SelectMany(x => x.GetGeometryIds()).ToList(), []);
    }
}
