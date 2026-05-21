using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.DebugViz;
using AIQ.Urban.RhinoInterface.Pipeline.Configs;
using AIQ.Urban.RhinoInterface.Tests.Helpers;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.RhinoInterface.Tests;

[RhinoTestFixture]
public class TransitPipelineConfigurationTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("SingleRoad")]
    [TestCase("MultipleTransitsDifferentTypes")]
    [TestCase("EmptyList")]
    public void WithTransitAdditionSteps_AddsTransitsToModel(string testName)
    {
        // Arrange
        var testData = _testData.Single(x => x.CaseName == testName);

        using var doc = RhinoDoc.CreateHeadless(null);
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
        var config = new TransitPipelineConfiguration(testData.Input, testData.CenterlineIds);

        // Act
        new PipelineConfigurations(pipeline)
            .WithTransitAdditionSteps(config)
            .WithMetricsCalculation(false);
        pipeline.Run();

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeTransits();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(testData.ExpectedTransitCount));
        Assert.That(UrbanModel.Instance.Parcels, Has.Count.EqualTo(testData.ExpectedParcelCount));

        var items = RhinoDocHelpers.GetObjectCountByLayer(doc);
        if (testData.ExpectedRhinoLayerCounts is { Count: > 0 })
        {
            foreach (var (layer, count) in testData.ExpectedRhinoLayerCounts)
                RhinoDocAssertions.AssertLayerObjectCount(doc, layer, count);
        }
    }

    private record TransitPipelineTestData(
        string CaseName,
        List<TransitData> Input,
        List<Guid> CenterlineIds,
        int ExpectedTransitCount,
        int ExpectedParcelCount,
        Dictionary<string, int> ExpectedRhinoLayerCounts = null);

    private static readonly List<TransitPipelineTestData> _testData = BuildTestData();

    private static List<TransitPipelineTestData> BuildTestData()
    {
        var (data1, id1) = MakeRoadDataWithId(new(0, 0, 0), new(50, 0, 0), 6, TransitType.Road);
        var (data2a, id2a) = MakeRoadDataWithId(new(0, 0, 0), new(50, 0, 0), 6, TransitType.Road);
        var (data2b, id2b) = MakeRoadDataWithId(new(0, 10, 0), new(30, 10, 0), 4, TransitType.CyclePath);

        return
        [
            new TransitPipelineTestData(
                CaseName: "SingleRoad",
                Input: [data1],
                CenterlineIds: [id1],
                ExpectedTransitCount: 1,
                ExpectedParcelCount: 0,
                ExpectedRhinoLayerCounts: new Dictionary<string, int>
                {
                    ["AIQ Urban::Transit::Road::Outline::6"] = 1,
                    ["AIQ Urban::Transit::Road::Centerline::6"] = 1,
                    ["AIQ Urban::Transit::Road::Surface"] = 1,
                }),

            new TransitPipelineTestData(
                CaseName: "MultipleTransitsDifferentTypes",
                Input: [data2a, data2b],
                CenterlineIds: [id2a, id2b],
                ExpectedTransitCount: 2,
                ExpectedParcelCount: 0,
                ExpectedRhinoLayerCounts: new Dictionary<string, int>
                {
                    ["AIQ Urban::Transit::Road::Outline::6"] = 1,
                    ["AIQ Urban::Transit::Road::Centerline::6"] = 1,
                    ["AIQ Urban::Transit::Road::Surface"] = 1,
                    ["AIQ Urban::Transit::CyclePath::Outline::4"] = 1,
                    ["AIQ Urban::Transit::CyclePath::Centerline::4"] = 1,
                    ["AIQ Urban::Transit::CyclePath::Surface"] = 1,
                }),

            new TransitPipelineTestData(
                CaseName: "EmptyList",
                Input: [],
                CenterlineIds: [],
                ExpectedTransitCount: 0,
                ExpectedParcelCount: 0),
        ];
    }

    private static (TransitData Data, Guid Id) MakeRoadDataWithId(Point3d from, Point3d to, double width, TransitType type)
    {
        var id = Guid.NewGuid();
        return (new TransitData(new Curve2D(new LineCurve(from, to), id), width, type), id);
    }
}
