using ArchSitePlan.BHoM.Geometry.Models;
using BH.Engine.Geometry;

namespace ArchSitePlan.Core.Factories.Models;

public static class SampleBuildingDefinition
{
    public static BuildingDefinition DetachedHouse = BuildingDefinition.FromFootprint(
        buildingFootprint: Create.PolyCurve([
            new Rectangle(
                Create.Point(-6,2),
                Create.Point(6,20))
            ]),
        floorHeight: 3.6,
        floorCount: 3,
        frontageWidth: 20);

    public static BuildingDefinition HugeMansion = BuildingDefinition.FromFootprint(
        buildingFootprint: Create.PolyCurve([
            new Rectangle(
                Create.Point(-6,2),
                Create.Point(6,60))
            ]),
        floorHeight: 3.6,
        floorCount: 20,
        frontageWidth: 20);

}
