namespace AIQ.Urban.Core.ModelActions;

public class RemoveBuildingStep(List<Guid> _geometryIds) : IDeleteUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model)
    {
        var buildingsToRemove = model.Buildings
            .Where(x => x.GetGeometryIds()
                .Any(id => _geometryIds.Contains(id)))
            .ToList();

        model.Buildings.RemoveAll(x => buildingsToRemove.Contains(x));

        return buildingsToRemove.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}
