using ArchSitePlan.Core.Factories;
using ArchSitePlan.Core.Pipeline.CreationData;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Test.ArchSitePlan.Core.Factories;

public class RectangularBuildingFactoryTests
{
    private Polygon _narrowSite = Create.Polygon([
        Create.Point(0,0),
            Create.Point(200,0),
            Create.Point(400, 300),
            Create.Point(225, 150),
            Create.Point(0, 100),
            Create.Point(-150, 150),
            Create.Point(-50,20)
    ]);

    private Polygon _twistingSite = Create.Polygon([
        Create.Point(0, 100),
        Create.Point(50, 100),
        Create.Point(120, 40),
        Create.Point(160, 40),
        Create.Point(170, 130),
        Create.Point(200, 120),
        Create.Point(180, 0),
        Create.Point(120, 0),
        Create.Point(50, 60),
        Create.Point(0, 60)]);

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
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 50000,
            TargetFAR: null,
            Setback: 6,
            MaxHeight: 24,
            MaxLength: 50,
            MinLength: 10,
            FloorHeight: 3.6);

        // Test Line Boolean Intersections
        // Then test building

        var building = new RectangleBuildingFactory(config)
            .Generate(Create.PolyCurve([siteBoundary]));

        var gfa = building.Buildings.Sum(x => x.BUA());
        Assert.Equal(50000, gfa, 1000.0);
    }



    [Fact]
    public void GenerateRectangleBuilding_OddSite()
    {
        // Arrange
        var siteBoundary = _narrowSite;

        var config = new AutoRectangleBuildingData(
            BuildingWidth: 15,
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 50000,
            TargetFAR: null,
            Setback: 6,
            MaxHeight: 24,
            MaxLength: 50,
            MinLength: 10,
            FloorHeight: 3.6);

        // Act
        var layout = new RectangleBuildingFactory(config)
            .Generate(Create.PolyCurve([siteBoundary]));

        var gfa = layout.Buildings.Sum(x => x.BUA());

        // Visualize
        TestVisualizers.VisualizeBuildingLayout(layout);

        // Assert
        Assert.Equal(50000, gfa, 1000.0);
    }

    [Fact]
    public void GenerateRectangleBuilding_OddSite_DeepFloorPlate()
    {
        // Arrange
        var siteBoundary = _narrowSite;

        var config = new AutoRectangleBuildingData(
            BuildingWidth: 25,
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 50000,
            TargetFAR: null,
            Setback: 6,
            MaxHeight: 24,
            MaxLength: 50,
            MinLength: 10,
            FloorHeight: 3.6);

        // Act
        var layout = new RectangleBuildingFactory(config)
            .Generate(Create.PolyCurve([siteBoundary]));

        var gfa = layout.Buildings.Sum(x => x.BUA());

        // Visualize
        TestVisualizers.VisualizeBuildingLayout(layout);

        // Assert
        Assert.Equal(50000, gfa, 1000.0);
    }

    [Fact]
    public void GenerateRectangleBuilding_TwistedSite()
    {
        // Arrange
        var siteBoundary = _twistingSite;

        var config = new AutoRectangleBuildingData(
            BuildingWidth: 13,
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 50000,
            TargetFAR: null,
            Setback: 6,
            MaxHeight: 24,
            MaxLength: 50,
            MinLength: 10,
            FloorHeight: 3.6);

        // Act
        var layout = new RectangleBuildingFactory(config)
            .Generate(Create.PolyCurve([siteBoundary]));

        var gfa = layout.Buildings.Sum(x => x.BUA());

        // Visualize
        TestVisualizers.VisualizeBuildingLayout(layout);

        // Assert
        Assert.Equal(50000, gfa, 1000.0);
    }
}
