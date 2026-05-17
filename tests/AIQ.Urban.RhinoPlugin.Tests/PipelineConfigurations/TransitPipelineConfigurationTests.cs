using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Pipeline.Configs;
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
        var testData = _testData.Single(x => x.CaseName == testName);

        using var doc = RhinoDoc.CreateHeadless(null);
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
        var config = new TransitPipelineConfiguration(testData.Input, testData.CenterlineIds);

        new PipelineConfigurations(pipeline)
            .WithTransitAdditionSteps(config)
            .WithMetricsCalculation(false);
        pipeline.Run();

        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(testData.ExpectedTransitCount));
        Assert.That(UrbanModel.Instance.Parcels, Has.Count.EqualTo(testData.ExpectedParcelCount));
    }

    [Test]
    public void WithTransitAdditionSteps_DuplicateCenterlineIds_RemovesOldAndRecalculatesParcels()
    {
        var ids = new Guid[4];
        var ringData = new List<TransitData>
        {
            MakeRoadData(new(0, 0, 0), new(30, 0, 0), 6, TransitType.Road, out ids[0]),
            MakeRoadData(new(30, 0, 0), new(30, 30, 0), 6, TransitType.Road, out ids[1]),
            MakeRoadData(new(30, 30, 0), new(0, 30, 0), 6, TransitType.Road, out ids[2]),
            MakeRoadData(new(0, 30, 0), new(0, 0, 0), 6, TransitType.Road, out ids[3]),
        };
        var config = new TransitPipelineConfiguration(ringData, [.. ids]);

        using var doc = RhinoDoc.CreateHeadless(null);

        // First run: add ring of roads → creates 1 parcel
        var pipeline1 = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
        new PipelineConfigurations(pipeline1)
            .WithTransitAdditionSteps(config)
            .WithMetricsCalculation(false);
        pipeline1.Run();

        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(4));
        Assert.That(UrbanModel.Instance.Parcels, Has.Count.EqualTo(1));

        // Second run: same centerline IDs → RemoveTransitStep removes old,
        // AddTransitStep re-adds, parcels recalculated
        var pipeline2 = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
        new PipelineConfigurations(pipeline2)
            .WithTransitAdditionSteps(config)
            .WithMetricsCalculation(false);
        pipeline2.Run();

        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(4));
        Assert.That(UrbanModel.Instance.Parcels, Has.Count.EqualTo(1));
    }

    private record TransitPipelineTestData(
        string CaseName,
        List<TransitData> Input,
        List<Guid> CenterlineIds,
        int ExpectedTransitCount,
        int ExpectedParcelCount);

    private static TransitData MakeRoadData(Point3d from, Point3d to, double width, TransitType type, out Guid id)
    {
        id = Guid.NewGuid();
        return new TransitData(new Curve2D(new LineCurve(from, to), id), width, type);
    }

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
                ExpectedParcelCount: 0),

            new TransitPipelineTestData(
                CaseName: "MultipleTransitsDifferentTypes",
                Input: [data2a, data2b],
                CenterlineIds: [id2a, id2b],
                ExpectedTransitCount: 2,
                ExpectedParcelCount: 0),

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
