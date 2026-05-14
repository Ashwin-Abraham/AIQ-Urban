using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;

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

public record BuildingData(
    Polygon2D Footprint,
    double TypicalFloorHeight,
    int FloorCount,
    Use Use)
{
    public Building ToBuilding()
    {
        return new Building(Footprint, TypicalFloorHeight, FloorCount, Use);
    }
}
