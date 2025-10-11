using AQI.Urban.Core.Tests.TestUtils;
using ArchSitePlan.Core.Factories;
using ArchSitePlan.Core.Pipeline.CreationData;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace AIQ.Urban.Core.Tests.Factories;

public class PeripheralBuildingFactoryTests
{
    Polygon _siteBoundary = Create.Polygon([
        Create.Point(0,0),
                Create.Point(200,0),
                Create.Point(225, 300),
                Create.Point(0,100)
    ]);

    public static IEnumerable<object[]> CreateData()
    {
        Polygon siteBoundary = Create.Polygon([
            Create.Point(0,0),
                        Create.Point(200,0),
                        Create.Point(225, 300),
                        Create.Point(0,100)
        ]);

        Polygon jaggedBoundary = Create.Polygon([
            Create.Point(0,0),
            Create.Point(200,0),
            Create.Point(225, 300),
            Create.Point(50, 280),
            Create.Point(-25, 300),
            Create.Point(0,100)
        ]);

        //yield return new object[]
        //{
        //    siteBoundary,
        //    69581        
        //};

        yield return new object[]
        {
            jaggedBoundary,
            69782
        };
    }

    [Theory]
    [MemberData(nameof(CreateData))]
    public void GeneratePeripheralBuilding(
        Polygon siteBoundary, 
        double expectedGfa)
    {
        // Arrange
        var config = new AutoPeripheralBuildingData(
            BuildingWidth: 15,
            MinFloorPlateDepth: 5,
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 70000,
            TargetFAR: null,
            Setback: 6,
            MaxHeight: 45,
            MaxLength: 50,
            MinLength: 15,
            FloorHeight: 3.6);

        // Act
        var layout = new PeripheralBuildingFactory(config)
            .Generate(Create.PolyCurve([siteBoundary]));

        var gfa = layout.Buildings.Sum(x => x.BUA());
        var floorCount = layout.Buildings.Select(x => x.FloorCount).ToList();

        // Visualize
        TestVisualizers.VisualizeBuildingLayout(layout);

        // Assert
        Assert.Equal(expectedGfa, gfa, 10.0);
    }

    [Fact]
    public void GeneratePeripheralBuilding_FARMode()
    {
        // Arrange
        var siteBoundary = _siteBoundary;
        var targetFAR = 4;

        var config = new AutoPeripheralBuildingData(
            BuildingWidth: 15,
            MinFloorPlateDepth: 5,
            TargetMode: BuildingTargetMode.FAR,
            TargetGFA: null,
            TargetFAR: 4,
            Setback: 6,
            MaxHeight: 45,
            MaxLength: 50,
            MinLength: 15,
            FloorHeight: 3.6);

        // Act
        var layout = new PeripheralBuildingFactory(config)
            .Generate(Create.PolyCurve([siteBoundary]));

        // Visualize
        TestVisualizers.VisualizeBuildingLayout(layout);

        // Assert
        var far = layout.Buildings.Sum(x => x.BUA()) / layout.SiteBoundary.IArea();
        Assert.Equal(targetFAR, far, 0.2);
    }
}
