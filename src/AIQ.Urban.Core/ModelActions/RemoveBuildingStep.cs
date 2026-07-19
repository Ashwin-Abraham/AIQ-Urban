using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class RemoveBuildingStep : IUrbanModelStep
{
    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var geometryIds = creationData.OfType<BuildingDeletionData>()
            .SelectMany(d => d.GeometryIds)
            .ToList();

        var buildingsToRemove = model.Buildings
            .Where(x => x.GetGeometryIds()
                .Any(id => geometryIds.Contains(id)))
            .ToList();

        model.Buildings.RemoveAll(x => buildingsToRemove.Contains(x));

        return new([], buildingsToRemove.SelectMany(x => x.GetGeometryIds()).ToList());
    }
}
