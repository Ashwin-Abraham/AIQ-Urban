using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.DebugViz;
using AIQ.Urban.RhinoInterface.EventHandler;
using AIQ.Urban.RhinoInterface.Pipeline.Outputters;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using AIQ.Urban.RhinoInterface.Tests.Helpers;
using AIQ.Urban.RhinoInterface.Utils;
using Microsoft.Extensions.Logging;
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
    [TestCase("WithInternalParcel")]
    public void WithTransitAdditionSteps_AddsTransitsToModel(string testName)
    {
        // Arrange
        var testData = _testData.Single(x => x.CaseName == testName);

        using var doc = RhinoDoc.CreateHeadless(null);
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);

        // Act
        pipeline
            .WithTransitAdditionSteps(testData.Input, testData.CenterlineIds)
            .WithFullModelOutput()
            .WithMetricsCalculation(false)
            .Run();

        // Visualise
#if DEBUG
        UrbanModel.Instance.VisualizeAll();
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

    [Test]
    public void WithTransitAdditionSteps_GatheredExistingCenterline_ReplacesRhinoObjectWithoutDuplicate()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var centerline = new LineCurve(new Point3d(0, 0, 0), new Point3d(50, 0, 0));
        var centerlineId = doc.Objects.AddCurve(centerline);
        var rhinoObject = doc.Objects.Find(centerlineId)!;
        var existingTransit = new TransitData(
            new Curve2D(centerline, centerlineId),
            6,
            TransitType.Road);
        UrbanModel.Instance.AddTransit([existingTransit.ToTransit()]);

        var gatherStep = new SelectTransitCenterlinesStep(
            [rhinoObject],
            6,
            TransitType.Road);

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(gatherStep)
            .WithTransitAdditionSteps([], [centerlineId])
            .WithFullModelOutput()
            .Run();

        // Assert
        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(1));
        Assert.That(doc.Objects.Count(), Is.EqualTo(4));
        Assert.That(doc.Objects.Find(centerlineId), Is.Null);

        var replacementCenterlineId = UrbanModel.Instance.Transits.Single().Centerline.Id;
        Assert.That(replacementCenterlineId, Is.Not.EqualTo(centerlineId));
        Assert.That(doc.Objects.Find(replacementCenterlineId), Is.Not.Null);
        RhinoDocAssertions.AssertLayerObjectCount(
            doc,
            "AIQ Urban::Transit::Road::Centerline::6",
            1);
    }

    [Test]
    public void SelectTransitCenterlinesStep_MixedGroundPlaneInputs_ReturnsDataAndWarningsPerCurve()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var validId = doc.Objects.AddLine(new Line(new Point3d(0, 0, 0), new Point3d(10, 0, 0)));
        var nearGroundId = doc.Objects.AddLine(new Line(new Point3d(0, 5, 0.000009), new Point3d(10, 5, -0.000009)));
        var invalidId = doc.Objects.AddLine(new Line(new Point3d(0, 10, 1), new Point3d(10, 10, 1)));
        var selected = new[] { validId, nearGroundId, invalidId }
            .Select(id => doc.Objects.FindId(id)!)
            .ToList();
        var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        var step = new SelectTransitCenterlinesStep(selected, 6, TransitType.Road, loggerFactory);

        var result = step.Run(doc);

        var transitData = result.OfType<TransitData>().ToList();
        var deletionIds = result.OfType<RhinoObjectDeletionData>().Select(x => x.Id).ToList();
        Assert.That(transitData, Has.Count.EqualTo(2));
        Assert.That(deletionIds, Is.EquivalentTo(new[] { validId, nearGroundId }));
        Assert.That(deletionIds, Does.Not.Contain(invalidId));
        Assert.That(transitData.SelectMany(x => x.Centerline.Geometry.Points()).Select(x => x.Z), Is.All.EqualTo(0));

        var warnings = loggerProvider.Entries.Where(x => x.Level == LogLevel.Warning).ToList();
        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0].Message, Does.Contain(invalidId.ToString()));
        Assert.That(warnings[0].Message, Does.Contain("Z range"));
    }

    [Test]
    public void WithTransitAdditionSteps_MixedGroundPlaneInputs_LeavesRejectedCurveUntouched()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var validId = doc.Objects.AddLine(new Line(new Point3d(0, 0, 0.000009), new Point3d(10, 0, 0.000009)));
        var invalidId = doc.Objects.AddLine(new Line(new Point3d(0, 10, 1), new Point3d(10, 10, 1)));
        var selected = new[] { validId, invalidId }.Select(id => doc.Objects.FindId(id)!).ToList();
        var gatherStep = new SelectTransitCenterlinesStep(selected, 6, TransitType.Road);

        var mutationCount = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(gatherStep)
            .WithTransitAdditionSteps()
            .WithFullModelOutput()
            .Run();

        Assert.That(mutationCount, Is.GreaterThan(0));
        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(1));
        Assert.That(doc.Objects.FindId(validId), Is.Null);
        Assert.That(doc.Objects.FindId(invalidId), Is.Not.Null);
        var centerlineBounds = UrbanModel.Instance.Transits.Single().Centerline.Geometry.GetBoundingBox(true);
        Assert.That(centerlineBounds.Min.Z, Is.EqualTo(0));
        Assert.That(centerlineBounds.Max.Z, Is.EqualTo(0));
    }

    [Test]
    public void WithTransitAdditionSteps_AllInputsInvalid_CompletesWithNoMutations()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var invalidId = doc.Objects.AddLine(new Line(new Point3d(0, 0, 1), new Point3d(10, 0, 1)));
        var gatherStep = new SelectTransitCenterlinesStep([doc.Objects.FindId(invalidId)!], 6, TransitType.Road);

        var mutationCount = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(gatherStep)
            .WithTransitAdditionSteps()
            .WithFullModelOutput()
            .Run();

        Assert.That(mutationCount, Is.EqualTo(0));
        Assert.That(UrbanModel.Instance.Transits, Is.Empty);
        Assert.That(doc.Objects.FindId(invalidId), Is.Not.Null);
    }

    [Test]
    public void WithTransitRestoreSteps_InvalidReplacement_RebakesLastValidTransit()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var oldCenterlineId = Guid.NewGuid();
        var oldTransit = new TransitData(
            new Curve2D(
                new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0)),
                oldCenterlineId),
            6,
            TransitType.Road).ToTransit();
        UrbanModel.Instance.AddTransit([oldTransit]);
        var rejectedId = doc.Objects.AddLine(new Line(new Point3d(0, 0, 1), new Point3d(10, 0, 1)));

        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithTransitRestoreSteps(oldCenterlineId, rejectedId)
            .WithRhinoOutputter(new TransitOutputter())
            .Run();

        Assert.That(doc.Objects.FindId(rejectedId), Is.Null);
        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(1));
        var restored = UrbanModel.Instance.Transits.Single();
        Assert.That(restored.Width, Is.EqualTo(6));
        Assert.That(restored.TransitType, Is.EqualTo(TransitType.Road));
        Assert.That(restored.Centerline.Id, Is.Not.EqualTo(oldCenterlineId));
        Assert.That(doc.Objects.FindId(restored.Centerline.Id), Is.Not.Null);
    }

    [Test]
    public void HandleTransitReplace_CenterlineOutsideGroundTolerance_RestoresLastValidTransit()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var oldCenterlineId = Guid.NewGuid();
        var oldTransit = new TransitData(
            new Curve2D(
                new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0)),
                oldCenterlineId),
            6,
            TransitType.Road).ToTransit();
        UrbanModel.Instance.AddTransit([oldTransit]);
        var rejectedId = doc.Objects.AddLine(new Line(new Point3d(0, 0, 1), new Point3d(10, 0, 1)));
        using var handler = new RhinoUrbanEventHandler(doc);

        handler.HandleTransitReplace(oldTransit, rejectedId);

        Assert.That(doc.Objects.FindId(rejectedId), Is.Null);
        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(1));
        var restored = UrbanModel.Instance.Transits.Single();
        Assert.That(restored.Width, Is.EqualTo(6));
        Assert.That(restored.TransitType, Is.EqualTo(TransitType.Road));
        Assert.That(restored.Centerline.Geometry.GetBoundingBox(true).Min.Z, Is.EqualTo(0));
        Assert.That(doc.Objects.FindId(restored.Centerline.Id), Is.Not.Null);
    }

    [Test]
    public void WithDocModelLoad_InvalidManagedCenterline_SkipsWarnsAndPreservesRhinoObject()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var attributes = RhinoOutputHelper.MakeLayerDrivenAttributes(
            doc,
            LayerConstants.TransitCenterline(TransitType.Road, 6),
            Color.Black);
        var validId = doc.Objects.AddLine(
            new Line(new Point3d(0, 0, 0), new Point3d(10, 0, 0)),
            attributes);
        var invalidId = doc.Objects.AddLine(
            new Line(new Point3d(0, 10, 1), new Point3d(10, 10, 1)),
            attributes);
        var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = new LoggerFactory([loggerProvider]);

        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, loggerFactory)
            .WithDocModelLoad(doc)
            .Run();

        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(1));
        Assert.That(doc.Objects.FindId(validId), Is.Null);
        Assert.That(doc.Objects.FindId(invalidId), Is.Not.Null);
        var warnings = loggerProvider.Entries.Where(x => x.Level == LogLevel.Warning).ToList();
        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0].Message, Does.Contain(invalidId.ToString()));
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
        var (data1, id1) = MakeRoadDataWithId(6, TransitType.Road, new(0, 0, 0), new(50, 0, 0));
        var (data2a, id2a) = MakeRoadDataWithId(6, TransitType.Road, new(0, 0, 0), new(50, 0, 0));
        var (data2b, id2b) = MakeRoadDataWithId(4, TransitType.CyclePath, new(0, 10, 0), new(30, 10, 0));
        var (uRoadData, uRoadId) = MakeRoadDataWithId(6, TransitType.Road,
            new(0, 30, 0), new(0, 0, 0), new(30, 0, 0), new(30, 30, 0));
        var (lineRoadData, lineRoadId) = MakeRoadDataWithId(6, TransitType.Road,
            new(0, 30, 0), new(30, 30, 0));

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

            new TransitPipelineTestData(
                CaseName: "WithInternalParcel",
                Input: [uRoadData, lineRoadData],
                CenterlineIds: [uRoadId, lineRoadId],
                ExpectedTransitCount: 2,
                ExpectedParcelCount: 1,
                ExpectedRhinoLayerCounts: new Dictionary<string, int>
                {
                    ["AIQ Urban::Transit::Road::Outline::6"] = 2,
                    ["AIQ Urban::Transit::Road::Centerline::6"] = 2,
                    ["AIQ Urban::Transit::Road::Surface"] = 1,
                    ["AIQ Urban::Parcel::Public"] = 1,
                }),
        ];
    }

    private static (TransitData Data, Guid Id) MakeRoadDataWithId(double width, TransitType type, params Point3d[] points)
    {
        var id = Guid.NewGuid();
        var curve = points.Length switch
        {
            2 => new Curve2D(new LineCurve(points[0], points[1]), id),
            > 2 => new Curve2D(new PolylineCurve(points), id),
            _ => throw new ArgumentException("Need at least 2 points", nameof(points))
        };
        return (new TransitData(curve, width, type), id);
    }
}
