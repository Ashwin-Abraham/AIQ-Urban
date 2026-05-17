using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.RhinoInterface.Pipeline.Configs;
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
        var testData = _testData.Single(x => x.CaseName == testName);

        using var doc = RhinoDoc.CreateHeadless(null);
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
        var config = new BuildingPipelineConfiguration(testData.Input, []);

        new PipelineConfigurations(pipeline)
            .WithBuildingAdditionSteps(config)
            .WithMetricsCalculation(false);
        pipeline.Run();

        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(testData.ExpectedCount));

        if (testData.ExpectedCount > 0)
            Assert.That(UrbanModel.Instance.Buildings.SelectMany(b => b.GetGeometryIds()), Is.Not.Empty);
    }

    private record BuildingPipelineTestData(
        string CaseName,
        List<BuildingData> Input,
        int ExpectedCount);

    private static readonly Polygon2D SimpleFootprint = new(
        new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(10, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));

    private static readonly Polygon2D SmallFootprint = new(
        new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(5, 0, 0),
            new Point3d(5, 5, 0),
            new Point3d(0, 5, 0),
            new Point3d(0, 0, 0),
        ]));

    private static readonly List<BuildingPipelineTestData> _testData =
    [
        new BuildingPipelineTestData(
            CaseName: "SingleBuilding",
            Input: [new BuildingData(SimpleFootprint, 3.5, 5, DefaultUses.Residential)],
            ExpectedCount: 1),

        new BuildingPipelineTestData(
            CaseName: "MultipleBuildings",
            Input:
            [
                new BuildingData(SimpleFootprint, 3.5, 5, DefaultUses.Residential),
                new BuildingData(SmallFootprint, 4.0, 3, DefaultUses.Commercial),
            ],
            ExpectedCount: 2),

        new BuildingPipelineTestData(
            CaseName: "EmptyList",
            Input: [],
            ExpectedCount: 0),
    ];
}
