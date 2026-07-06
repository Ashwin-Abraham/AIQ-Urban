using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record BuildingData(
    Polygon2D Footprint,
    double TypicalFloorHeight,
    int FloorCount,
    Use Use) : ICreationData
{
    public Building ToBuilding()
    {
        return new Building(Footprint, TypicalFloorHeight, FloorCount, Use);
    }
}
