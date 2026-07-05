using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class AddBuildingStep : IAddUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model, ICreationData? creationData)
    {
        var collection = creationData as CreationDataCollection<BuildingData>
            ?? throw new InvalidOperationException("Expected CreationDataCollection<BuildingData>.");

        var buildings = collection.Items.Select(x => x.ToBuilding()).ToList();
        foreach (var building in buildings)
            model.AddBuilding(building);

        return buildings.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}
