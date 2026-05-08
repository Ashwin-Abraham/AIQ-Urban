using AQI.Urban.Core.Tests.TestUtils;
using ArchSitePlan.Core.Factories;
using ArchSitePlan.Core.Models.CreationData;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace AIQ.Urban.Core.Tests.Factories;

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
        // Arrange
        var siteBoundary = Create.Polygon([
            Create.Point(0,0),
            Create.Point(200,0),
            Create.Point(225, 300),
            Create.Point(0,100)
        ]);

        var config = new PrimaryAxisBuildingData(
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
        var building = new PrimarySiteAxisBuildingFactory(config)
            .Generate(Create.PolyCurve([siteBoundary]));

        // Visualize
        TestVisualizers.VisualizeBuildingLayout(building);

        // Assert
        var gfa = building.Buildings.Sum(x => x.BUA());
        Assert.Equal(50000, gfa, 1000.0);
    }



    [Fact]
    public void GenerateRectangleBuilding_OddSite()
    {
        // Arrange
        var siteBoundary = _narrowSite;

        var config = new PrimaryAxisBuildingData(
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
        var layout = new PrimarySiteAxisBuildingFactory(config)
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

        var config = new PrimaryAxisBuildingData(
            BuildingWidth: 25,
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 50000,
            TargetFAR: null,
            Setback: 6,
            MaxHeight: 100,
            MaxLength: 50,
            MinLength: 10,
            FloorHeight: 3.6);

        // Act
        var layout = new PrimarySiteAxisBuildingFactory(config)
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

        var config = new PrimaryAxisBuildingData(
            BuildingWidth: 13,
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 50000,
            TargetFAR: null,
            Setback: 6,
            MaxHeight: 100,
            MaxLength: 50,
            MinLength: 10,
            FloorHeight: 3.6);

        // Act
        var layout = new PrimarySiteAxisBuildingFactory(config)
            .Generate(Create.PolyCurve([siteBoundary]));

        var gfa = layout.Buildings.Sum(x => x.BUA());

        // Visualize
        TestVisualizers.VisualizeBuildingLayout(layout);

        // Assert
        Assert.Equal(50000, gfa, 1000.0);
    }
}
