using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class AddBuildingStep(List<BuildingData> _buildingData) : IAddUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model)
    {
        var buildings = _buildingData.Select(x => x.ToBuilding()).ToList();
        foreach (var building in buildings)
            model.AddBuilding(building);

        return buildings.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}

