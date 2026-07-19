using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class RemoveZoneStep : IUrbanModelStep
{
    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var geometryIds = creationData.OfType<ZoneDeletionData>()
            .SelectMany(d => d.GeometryIds)
            .ToList();

        var zonesToRemove = model.Zones
            .Where(x => x.GetGeometryIds()
                .Any(id => geometryIds.Contains(id)))
            .ToList();

        model.Zones.RemoveAll(x => zonesToRemove.Contains(x));

        return new([], zonesToRemove.SelectMany(x => x.GetGeometryIds()).ToList());
    }
}
