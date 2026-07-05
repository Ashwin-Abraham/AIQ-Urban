using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.DebugViz;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.RhinoInterface.Tests;

[RhinoTestFixture]
public class BuildingPipelineConfigurationTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("SingleBuilding")]
    [TestCase("MultipleBuildings")]
    [TestCase("EmptyList")]
    public void WithBuildingAdditionSteps_AddsBuildingsToModel(string testName)
    {
        // Arrange
        var testData = _testData.Single(x => x.CaseName == testName);

        using var doc = RhinoDoc.CreateHeadless(null);
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);

        // Act
        pipeline
            .WithBuildingAdditionSteps(testData.Input, [])
            .WithMetricsCalculation(false)
            .Run();

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeBuildings();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(testData.ExpectedCount));

        if (testData.ExpectedCount > 0)
            Assert.That(UrbanModel.Instance.Buildings.SelectMany(b => b.GetGeometryIds()), Is.Not.Empty);
    }

    private record BuildingPipelineTestData(
        string CaseName,
        List<BuildingData> Input,
        int ExpectedCount);

    private static readonly Polygon2D Footprint1 = new(
        new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(10, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));

    private static readonly Polygon2D Footprint2 = new(
        new PolylineCurve([
            new Point3d(13, 13, 0),
            new Point3d(15, 13, 0),
            new Point3d(15, 15, 0),
            new Point3d(13, 15, 0),
            new Point3d(13, 13, 0),
        ]));

    private static readonly List<BuildingPipelineTestData> _testData =
    [
        new BuildingPipelineTestData(
            CaseName: "SingleBuilding",
            Input: [new BuildingData(Footprint1, 3.5, 5, DefaultUses.Residential)],
            ExpectedCount: 1),

        new BuildingPipelineTestData(
            CaseName: "MultipleBuildings",
            Input:
            [
                new BuildingData(Footprint1, 3.5, 5, DefaultUses.Residential),
                new BuildingData(Footprint2, 4.0, 3, DefaultUses.Commercial),
            ],
            ExpectedCount: 2),

        new BuildingPipelineTestData(
            CaseName: "EmptyList",
            Input: [],
            ExpectedCount: 0),
    ];
}
