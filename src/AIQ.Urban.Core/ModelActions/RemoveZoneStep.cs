namespace AIQ.Urban.Core.ModelActions;

public class RemoveZoneStep(List<Guid> _geometryIds) : IDeleteUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model)
    {
        var zonesToRemove = model.Zones
            .Where(x => x.GetGeometryIds()
                .Any(id => _geometryIds.Contains(id)))
            .ToList();

        model.Zones.RemoveAll(x => zonesToRemove.Contains(x));

        return zonesToRemove.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}
