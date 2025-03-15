using ArchSitePlan.Core.Factories;
using ArchSitePlan.Core.Pipeline.CreationData;
using BH.Engine.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Test.ArchSitePlan.Core.Factories;

public class RectangularBuildingFactoryTests
{

    [Fact]
    public void GenerateRectangleBuilding()
    {
        var siteBoundary = Create.Polygon([
            Create.Point(0,0),
            Create.Point(200,0),
            Create.Point(225, 300),
            Create.Point(0,100)
        ]);

        var config = new AutoRectangleBuildingData(
            BuildingWidth: 15,
            MinFloorPlateDepth: 5,
            SiteBoundary: Create.PolyCurve([siteBoundary]),
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 50000,
            TargetFAR: null,
            TargetHeight: null,
            Setback: 6,
            MaxHeight: 24,
            MaxLength: 50,
            MinLength: 10,
            FloorHeight: 3.6);

        // Test Line Boolean Intersections
        // Then test building

        var building = new RectangleBuildingFactory(config).Generate();

        var gfa = building.Buildings.Sum(x => x.BUA());
        Assert.Equal(50000, gfa, 1000.0);
    }

    [Fact]
    public void GenerateRectangleBuilding_OddSite()
    {
        var siteBoundary = Create.Polygon([
            Create.Point(0,0),
            Create.Point(200,0),
            Create.Point(400, 300),
            Create.Point(225, 150),
            Create.Point(0, 100),
            Create.Point(-150, 150),
            Create.Point(-50,20)
        ]);

        var config = new AutoRectangleBuildingData(
            BuildingWidth: 15,
            MinFloorPlateDepth: 5,
            SiteBoundary: Create.PolyCurve([siteBoundary]),
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 50000,
            TargetFAR: null,
            TargetHeight: null,
            Setback: 6,
            MaxHeight: 24,
            MaxLength: 50,
            MinLength: 10,
            FloorHeight: 3.6);

        // Test Line Boolean Intersections
        // Then test building

        var building = new RectangleBuildingFactory(config).Generate();

        var gfa = building.Buildings.Sum(x => x.BUA());
        Assert.Equal(50000, gfa, 1000.0);
    }
}
