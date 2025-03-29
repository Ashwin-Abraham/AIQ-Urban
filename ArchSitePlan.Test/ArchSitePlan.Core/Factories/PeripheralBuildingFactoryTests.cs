using ArchSitePlan.Core.Factories;
using ArchSitePlan.Core.Pipeline.CreationData;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace ArchSitePlan.Test.ArchSitePlan.Core.Factories;

public class PeripheralBuildingFactoryTests
{
    Polygon _siteBoundary = Create.Polygon([
        Create.Point(0,0),
                Create.Point(200,0),
                Create.Point(225, 300),
                Create.Point(0,100)
    ]);

    [Fact]
    public void GeneratePeripheralBuilding()
    {
        // Arrange
        var siteBoundary = _siteBoundary;

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
        Assert.Equal(70000, gfa, 2000.0);
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
