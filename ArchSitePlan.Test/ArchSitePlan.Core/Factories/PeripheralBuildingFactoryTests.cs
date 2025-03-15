using ArchSitePlan.Core.Factories;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.Engine.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Test.ArchSitePlan.Core.Factories;

public class PeripheralBuildingFactoryTests
{
    [Fact]
    public void GeneratePeripheralBuilding()
    {
        var siteBoundary = Create.Polygon([
            Create.Point(0,0),
            Create.Point(200,0),
            Create.Point(225, 300),
            Create.Point(0,100)
        ]);

        var config = new AutoPeripheralBuildingData(
            BuildingWidth: 15,
            MinFloorPlateDepth: 5,
            SiteBoundary: Create.PolyCurve([siteBoundary]),
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 70000,
            TargetFAR: null,
            TargetHeight: null,
            Setback: 6,
            MaxHeight: 24,
            MaxLength: 50,
            MinLength: 15,
            FloorHeight: 3.6);

        // Test Line Boolean Intersections
        // Then test building
        var building = new PeripheralBuildingFactory(config)
            .Generate(Create.PolyCurve([siteBoundary]));

        var gfa = building.Buildings.Sum(x => x.BUA());
        var floorCount = building.Buildings.Select(x => x.FloorCount).ToList();

        DEBUGMODEL.Reset();
        DEBUGMODEL.Debug(building.Buildings.Select(x => x.Envelope.ToHypar()));
        DEBUGMODEL.OpenAsGlb();
        Assert.Equal(70000, gfa, 2000.0);
    }
}
