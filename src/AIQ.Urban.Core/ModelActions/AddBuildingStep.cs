using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class AddBuildingStep : IAddUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model, List<ICreationData> creationData)
    {
        var buildings = creationData.OfType<BuildingData>().Select(x => x.ToBuilding()).ToList();
        foreach (var building in buildings)
            model.AddBuilding(building);

        return buildings.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}
