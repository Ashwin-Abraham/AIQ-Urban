using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.DebugViz;
using AIQ.Urban.RhinoInterface;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.RhinoInterface.Tests;

[RhinoTestFixture]
public class ZonePipelineConfigurationTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("SingleZone")]
    [TestCase("MultipleZones")]
    [TestCase("EmptyList")]
    public void WithZoneAdditionSteps_AddsZonesToModel(string testName)
    {
        // Arrange
        var testData = _testData.Single(x => x.CaseName == testName);

        using var doc = RhinoDoc.CreateHeadless(null);

        // Act
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithZoneAdditionSteps(testData.Input, [])
            .WithMetricsCalculation(false);
        pipeline.Run();

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeZones();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Zones, Has.Count.EqualTo(testData.ExpectedCount));

        if (testData.ExpectedCount > 0)
            Assert.That(UrbanModel.Instance.Zones.SelectMany(z => z.GetGeometryIds()), Is.Not.Empty);
    }

    private record ZonePipelineTestData(
        string CaseName,
        List<ZoneData> Input,
        int ExpectedCount);

    private static readonly Polygon2D BigBoundary = new(
        new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(100, 0, 0),
            new Point3d(100, 100, 0),
            new Point3d(0, 100, 0),
            new Point3d(0, 0, 0),
        ]));

    private static readonly Polygon2D SmallBoundary = new(
        new PolylineCurve([
            new Point3d(10, 10, 0),
            new Point3d(30, 10, 0),
            new Point3d(30, 30, 0),
            new Point3d(10, 30, 0),
            new Point3d(10, 10, 0),
        ]));

    private static readonly List<ZonePipelineTestData> _testData =
    [
        new ZonePipelineTestData(
            CaseName: "SingleZone",
            Input: [new ZoneData(BigBoundary, "Downtown")],
            ExpectedCount: 1),

        new ZonePipelineTestData(
            CaseName: "MultipleZones",
            Input:
            [
                new ZoneData(BigBoundary, "Downtown"),
                new ZoneData(SmallBoundary, "Park"),
            ],
            ExpectedCount: 2),

        new ZonePipelineTestData(
            CaseName: "EmptyList",
            Input: [],
            ExpectedCount: 0),
    ];
}
