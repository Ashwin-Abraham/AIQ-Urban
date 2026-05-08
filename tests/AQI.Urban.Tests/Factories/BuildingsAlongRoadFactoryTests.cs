using AQI.Urban.Core.Tests.TestUtils;
using ArchSitePlan.Core.Factories;
using ArchSitePlan.Core.Factories.Models;
using ArchSitePlan.Core.Models.CreationData;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Core.Constants.Enums;

namespace AIQ.Urban.Core.Tests.Factories;

public class BuildingsAlongRoadFactoryTests
{
    private static Polygon _siteBoundary = Create.Polygon([
           Create.Point(0, 0),
            Create.Point(200, 0),
            Create.Point(225, 300),
            Create.Point(0, 100)
       ]);
    
    
    [Fact]
    public void CanCreateBuildingsAlongRoad()
    {
        // Arrange
        var siteBoundary = _siteBoundary;
        var fragments = _siteBoundary.ISubParts().ToList();

        var generatorEdge = Create.PolyCurve([fragments[0]]);

        var definitions = new List<BuildingDefinition>() { SampleBuildingDefinition.DetachedHouse };

        var config = new BuildingAlongRoadConfig(
            Definitions: definitions,
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 4000,
            TargetFAR: 1,
            Setback: 3);

        // Act
        var building = new BuildingsAlongRoadFactory(config)
            .Generate(Create.PolyCurve([siteBoundary]), generatorEdge);

        // Visualize
        DEBUGMODEL.Reset();
        DEBUGMODEL.Debug(building.Buildings.Select(x => x.Envelope.ToHypar()));
        siteBoundary.Debug();
        generatorEdge.Debug(DebugMaterials.Red, 1);
        DEBUGMODEL.OpenAsGlb();

        // Assert
        var gfa = building.Buildings.Sum(x => x.BUA());
        var floorCount = building.Buildings.Select(x => x.FloorCount).ToList();
        
        Assert.Equal(4536, gfa, 1.0);
    }

    [Fact]
    public void CanCreateBuildingsAlongRoad_HugeBuildingPartlyPlaced()
    {
        // Arrange
        var siteBoundary = _siteBoundary;
        var fragments = _siteBoundary.ISubParts().ToList();

        var generatorEdge = Create.PolyCurve([fragments[0]]);

        var definitions = new List<BuildingDefinition>() { 
            SampleBuildingDefinition.DetachedHouse,
            SampleBuildingDefinition.HugeMansion,
        };

        var config = new BuildingAlongRoadConfig(
            Definitions: definitions,
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 50000,
            TargetFAR: 1,
            Setback: 3);

        // Act
        var building = new BuildingsAlongRoadFactory(config)
            .Generate(Create.PolyCurve([siteBoundary]), generatorEdge);

        // Visualize
        DEBUGMODEL.Reset();
        DEBUGMODEL.Debug(building.Buildings.Select(x => x.Envelope.ToHypar()));
        siteBoundary.Debug();
        generatorEdge.Debug(DebugMaterials.Red, 1);
        DEBUGMODEL.OpenAsGlb();

        // Assert
        var gfa = building.Buildings.Sum(x => x.BUA());
        var floorCount = building.Buildings.Select(x => x.FloorCount).ToList();

        Assert.Equal(46296, gfa, 1.0);
    }

    [Fact]
    public void CanCreateBuildingsAlongRoad_ComplexSiteCurves()
    {
        // Arrange
        var arc = Create.Arc(Create.Point(150, 75), Create.Point(200, 100), Create.Point(50, 125));
        var siteBoundary = Create.PolyCurve([
            Create.Polyline([Point.Origin, Create.Point(100,0), Create.Point(150, 75)]),
            arc,
            Create.Line(Create.Point(50,125), Point.Origin)
        ]);

        var fragments = siteBoundary.ISubParts().ToList();

        var generatorEdge = Create.PolyCurve([
            fragments[0],
            fragments[1],
            fragments[2]]);

        var definitions = new List<BuildingDefinition>() {
            SampleBuildingDefinition.DetachedHouse
        };

        var config = new BuildingAlongRoadConfig(
            Definitions: definitions,
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 5000,
            TargetFAR: 1,
            Setback: 3);

        // Act
        var building = new BuildingsAlongRoadFactory(config)
            .Generate(Create.PolyCurve([siteBoundary]), generatorEdge);

        // Visualize
        DEBUGMODEL.Reset();
        TestVisualizers.DebugBuildingLayout(building);
        siteBoundary.Debug();
        generatorEdge.Debug(DebugMaterials.Red, 1);
        DEBUGMODEL.OpenAsGlb();

        // Assert
        var gfa = building.Buildings.Sum(x => x.BUA());
        var floorCount = building.Buildings.Select(x => x.FloorCount).ToList();

        Assert.Equal(20736, gfa, 1.0);
    }

    [Fact]
    public void CanCreateBuildingsAlongRoad_AvoidsClashes()
    {
        // Arrange
        var siteBoundary = _siteBoundary;
        var fragments = _siteBoundary.ISubParts().ToList();

        var generatorEdge = Create.PolyCurve([
            fragments[0],
            fragments[1],
            fragments[2]]);

        var definitions = new List<BuildingDefinition>() {
            SampleBuildingDefinition.DetachedHouse
        };

        var config = new BuildingAlongRoadConfig(
            Definitions: definitions,
            TargetMode: BuildingTargetMode.GFA,
            TargetGFA: 50000,
            TargetFAR: 1,
            Setback: 3);

        // Act
        var building = new BuildingsAlongRoadFactory(config)
            .Generate(Create.PolyCurve([siteBoundary]), generatorEdge);

        // Visualize
#if DEBUG
        DEBUGMODEL.Reset();
        DEBUGMODEL.Debug(building.Buildings.Select(x => x.Envelope.ToHypar()));
        siteBoundary.Debug();
        generatorEdge.Debug(DebugMaterials.Red, 1);
        DEBUGMODEL.OpenAsGlb();
#endif

        // Assert
        var gfa = building.Buildings.Sum(x => x.BUA());
        var floorCount = building.Buildings.Select(x => x.FloorCount).ToList();

        Assert.Equal(23328, gfa, 1.0);
    }


}
