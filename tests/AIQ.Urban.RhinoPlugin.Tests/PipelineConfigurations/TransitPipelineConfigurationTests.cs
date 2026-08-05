using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.DebugViz;
using AIQ.Urban.RhinoInterface.EventHandler;
using AIQ.Urban.RhinoInterface.Pipeline.EditData;
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
            .WithDataGatherStep(new TransitAdditionGatherStep(testData.Input, testData.CenterlineIds))
            .WithTransitAdditionSteps()
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
    [TestCase("SameAttributes")]
    [TestCase("ChangedAttributes")]
    public void WithTransitAdditionSteps_ManagedCenterline_ReplacesTransitWithoutDuplicate(string testName)
    {
        // Arrange
        var testData = GetManagedReplacementTestData(testName);
        using var doc = RhinoDoc.CreateHeadless(null);
        var points = testData.CenterlineCoordinates
            .Select(point => new Point3d(point[0], point[1], point[2]))
            .ToArray();
        var sourceId = doc.Objects.AddCurve(new PolylineCurve(points));

        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(new SelectTransitCenterlinesStep(
                [doc.Objects.FindId(sourceId)!],
                testData.ExistingWidth,
                (TransitType)testData.ExistingTransitType))
            .WithTransitAdditionSteps()
            .WithFullModelOutput()
            .Run();

        var existingTransit = UrbanModel.Instance.Transits.Single();
        var oldGeometryIds = existingTransit.GetGeometryIds().ToList();
        var managedCenterline = doc.Objects.FindId(existingTransit.Centerline.Id)!;

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(new SelectTransitCenterlinesStep(
                [managedCenterline],
                testData.ReplacementWidth,
                (TransitType)testData.ReplacementTransitType))
            .WithTransitAdditionSteps()
            .WithFullModelOutput()
            .Run();

        // Preview
#if DEBUG
        UrbanModel.Instance.VisualizeTransits();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(1));
        var replacement = UrbanModel.Instance.Transits.Single();
        Assert.That(replacement.Width, Is.EqualTo(testData.ReplacementWidth));
        Assert.That(replacement.TransitType, Is.EqualTo((TransitType)testData.ReplacementTransitType));
        Assert.That(replacement.Centerline.Id, Is.Not.EqualTo(existingTransit.Centerline.Id));
        Assert.That(oldGeometryIds.All(id => doc.Objects.FindId(id) is null), Is.True);
        Assert.That(doc.Objects.FindId(replacement.Centerline.Id), Is.Not.Null);
        RhinoDocAssertions.AssertLayerObjectCount(doc, testData.ExpectedCenterlineLayer, 1);
    }

    [Test]
    [TestCase("DistinctValidObjects")]
    [TestCase("MixedValidAndInvalidObjects")]
    [TestCase("RepeatedObject")]
    [TestCase("AllRejectedObjects")]
    public void SelectTransitCenterlinesStep_Selection_ReturnsExpectedEditData(string testName)
    {
        // Arrange
        var testData = GetTransitGatherTestData(testName);
        using var doc = RhinoDoc.CreateHeadless(null);
        var objectIds = testData.CurveCoordinates
            .Select(coordinates => doc.Objects.AddCurve(new PolylineCurve(
                coordinates.Select(point => new Point3d(point[0], point[1], point[2])))))
            .ToList();
        var selected = testData.SelectedCurveIndices
            .Select(index => doc.Objects.FindId(objectIds[index])!)
            .ToList();
        var expectedAcceptedIds = testData.ExpectedAcceptedCurveIndices
            .Select(index => objectIds[index])
            .ToList();
        var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        var step = new SelectTransitCenterlinesStep(selected, 6, TransitType.Road, loggerFactory);

        // Act
        var result = step.Run(doc);

        // Preview
        TestContext.WriteLine($"Accepted identities: {string.Join(", ", expectedAcceptedIds)}");

        // Assert
        var transitData = result.OfType<TransitData>().ToList();
        var rhinoDeletionIds = result.OfType<RhinoObjectDeletionData>().Select(data => data.Id).ToList();
        var transitDeletionData = result.OfType<TransitDeletionData>().ToList();
        Assert.That(transitData, Has.Count.EqualTo(expectedAcceptedIds.Count));
        Assert.That(rhinoDeletionIds, Is.EquivalentTo(expectedAcceptedIds));
        Assert.That(rhinoDeletionIds, Is.Unique);
        if (expectedAcceptedIds.Count > 0)
        {
            Assert.That(transitDeletionData, Has.Count.EqualTo(1));
            Assert.That(transitDeletionData.Single().CenterlineIds, Is.EquivalentTo(expectedAcceptedIds));
            Assert.That(transitDeletionData.Single().CenterlineIds, Is.Unique);
        }
        else
        {
            Assert.That(transitDeletionData, Is.Empty);
        }
        Assert.That(
            transitData.SelectMany(data => data.Centerline.Geometry.Points()).Select(point => point.Z),
            Is.All.EqualTo(0));

        var warnings = loggerProvider.Entries.Where(entry => entry.Level == LogLevel.Warning).ToList();
        Assert.That(warnings, Has.Count.EqualTo(testData.ExpectedWarningCount));
        if (testData.ExpectedWarningText.Length > 0)
            Assert.That(warnings.Single().Message, Does.Contain(testData.ExpectedWarningText));
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
    [TestCase("OutsideGroundTolerance")]
    [TestCase("SelfIntersecting")]
    public void WithTransitAdditionSteps_RejectedManagedCenterline_PreservesExistingTransit(string testName)
    {
        // Arrange
        var testData = GetRejectedManagedTransitTestData(testName);
        using var doc = RhinoDoc.CreateHeadless(null);
        var rejectedCurve = new PolylineCurve(testData.RejectedCoordinates
            .Select(point => new Point3d(point[0], point[1], point[2])));
        var managedId = doc.Objects.AddCurve(rejectedCurve);
        var existingTransit = new TransitData(
            new Curve2D(
                new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0)),
                managedId),
            6,
            TransitType.Road).ToTransit();
        UrbanModel.Instance.AddTransit([existingTransit]);
        var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = new LoggerFactory([loggerProvider]);

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, loggerFactory)
            .WithDataGatherStep(new SelectTransitCenterlinesStep(
                [doc.Objects.FindId(managedId)!],
                4,
                TransitType.CyclePath,
                loggerFactory))
            .WithTransitAdditionSteps()
            .WithFullModelOutput()
            .Run();

        // Preview
        TestContext.WriteLine($"Preserved Transit ID: {existingTransit.Centerline.Id}");

        // Assert
        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(1));
        Assert.That(UrbanModel.Instance.Transits.Single(), Is.SameAs(existingTransit));
        Assert.That(existingTransit.Width, Is.EqualTo(6));
        Assert.That(existingTransit.TransitType, Is.EqualTo(TransitType.Road));
        Assert.That(existingTransit.Centerline.Geometry.GetBoundingBox(true).Min.Z, Is.EqualTo(0));
        Assert.That(doc.Objects.FindId(managedId), Is.Not.Null);
        var warning = loggerProvider.Entries.Single(entry => entry.Level == LogLevel.Warning);
        Assert.That(warning.Message, Does.Contain(testData.ExpectedWarningText));
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
    public void WithTransitAdditionSteps_MixedSelfIntersectionInputs_LeavesRejectedCurveUntouched()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var validId = doc.Objects.AddLine(new Line(new Point3d(0, 0, 0), new Point3d(10, 0, 0)));
        var invalidId = doc.Objects.AddCurve(SelfIntersectingCenterline());
        var selected = new[] { validId, invalidId }.Select(id => doc.Objects.FindId(id)!).ToList();
        var gatherStep = new SelectTransitCenterlinesStep(selected, 6, TransitType.Road);

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(gatherStep)
            .WithTransitAdditionSteps()
            .WithFullModelOutput()
            .Run();

        // Assert
        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(1));
        Assert.That(doc.Objects.FindId(validId), Is.Null);
        Assert.That(doc.Objects.FindId(invalidId), Is.Not.Null);
    }

    [Test]
    public void WithTransitAdditionSteps_AllInputsSelfIntersecting_CompletesWithNoMutations()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var invalidId = doc.Objects.AddCurve(SelfIntersectingCenterline());
        var gatherStep = new SelectTransitCenterlinesStep(
            [doc.Objects.FindId(invalidId)!],
            6,
            TransitType.Road);

        // Act
        var mutationCount = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(gatherStep)
            .WithTransitAdditionSteps()
            .WithFullModelOutput()
            .Run();

        // Assert
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
            .WithDataGatherStep(new TransitRestoreGatherStep(oldCenterlineId, rejectedId))
            .WithTransitRestoreSteps()
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
    public void HandleTransitReplace_SelfIntersectingCenterline_RestoresLastValidTransit()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var oldCenterlineId = Guid.NewGuid();
        var oldTransit = new TransitData(
            new Curve2D(
                new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0)),
                oldCenterlineId),
            6,
            TransitType.Road).ToTransit();
        UrbanModel.Instance.AddTransit([oldTransit]);
        var rejectedId = doc.Objects.AddCurve(SelfIntersectingCenterline());
        using var handler = new RhinoUrbanEventHandler(doc);

        // Act
        handler.HandleTransitReplace(oldTransit, rejectedId);

        // Assert
        Assert.That(doc.Objects.FindId(rejectedId), Is.Null);
        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(1));
        var restored = UrbanModel.Instance.Transits.Single();
        Assert.That(restored.Width, Is.EqualTo(6));
        Assert.That(restored.TransitType, Is.EqualTo(TransitType.Road));
        Assert.That(restored.Centerline.Id, Is.Not.EqualTo(oldCenterlineId));
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

    private record ManagedReplacementTestData(
        string CaseName,
        double[][] CenterlineCoordinates,
        double ExistingWidth,
        int ExistingTransitType,
        double ReplacementWidth,
        int ReplacementTransitType,
        string ExpectedCenterlineLayer);

    private record TransitGatherTestData(
        string CaseName,
        double[][][] CurveCoordinates,
        int[] SelectedCurveIndices,
        int[] ExpectedAcceptedCurveIndices,
        int ExpectedWarningCount,
        string ExpectedWarningText);

    private record RejectedManagedTransitTestData(
        string CaseName,
        double[][] RejectedCoordinates,
        string ExpectedWarningText);

    private static RejectedManagedTransitTestData GetRejectedManagedTransitTestData(string caseName)
    {
        return caseName switch
        {
            "OutsideGroundTolerance" => new RejectedManagedTransitTestData(
                CaseName: "OutsideGroundTolerance",
                RejectedCoordinates: [[0, 0, 1], [10, 0, 1]],
                ExpectedWarningText: "Z range"),

            "SelfIntersecting" => new RejectedManagedTransitTestData(
                CaseName: "SelfIntersecting",
                RejectedCoordinates: [[0, 0, 0], [10, 10, 0], [0, 10, 0], [10, 0, 0]],
                ExpectedWarningText: "self-intersects"),

            _ => throw new ArgumentException($"Unknown test case: {caseName}")
        };
    }

    private static TransitGatherTestData GetTransitGatherTestData(string caseName)
    {
        return caseName switch
        {
            "DistinctValidObjects" => new TransitGatherTestData(
                CaseName: "DistinctValidObjects",
                CurveCoordinates: [
                    [[0, 0, 0], [10, 0, 0]],
                    [[0, 0, 0], [10, 0, 0]],
                ],
                SelectedCurveIndices: [0, 1],
                ExpectedAcceptedCurveIndices: [0, 1],
                ExpectedWarningCount: 0,
                ExpectedWarningText: ""),

            "MixedValidAndInvalidObjects" => new TransitGatherTestData(
                CaseName: "MixedValidAndInvalidObjects",
                CurveCoordinates: [
                    [[0, 0, 0], [10, 0, 0]],
                    [[0, 10, 1], [10, 10, 1]],
                ],
                SelectedCurveIndices: [0, 1],
                ExpectedAcceptedCurveIndices: [0],
                ExpectedWarningCount: 1,
                ExpectedWarningText: "Z range"),

            "RepeatedObject" => new TransitGatherTestData(
                CaseName: "RepeatedObject",
                CurveCoordinates: [
                    [[0, 0, 0], [10, 0, 0]],
                ],
                SelectedCurveIndices: [0, 0],
                ExpectedAcceptedCurveIndices: [0],
                ExpectedWarningCount: 0,
                ExpectedWarningText: ""),

            "AllRejectedObjects" => new TransitGatherTestData(
                CaseName: "AllRejectedObjects",
                CurveCoordinates: [
                    [[0, 0, 1], [10, 0, 1]],
                ],
                SelectedCurveIndices: [0],
                ExpectedAcceptedCurveIndices: [],
                ExpectedWarningCount: 1,
                ExpectedWarningText: "Z range"),

            _ => throw new ArgumentException($"Unknown test case: {caseName}")
        };
    }

    private static ManagedReplacementTestData GetManagedReplacementTestData(string caseName)
    {
        return caseName switch
        {
            "SameAttributes" => new ManagedReplacementTestData(
                CaseName: "SameAttributes",
                CenterlineCoordinates: [[0, 0, 0], [50, 0, 0]],
                ExistingWidth: 6,
                ExistingTransitType: (int)TransitType.Road,
                ReplacementWidth: 6,
                ReplacementTransitType: (int)TransitType.Road,
                ExpectedCenterlineLayer: "AIQ Urban::Transit::Road::Centerline::6"),

            "ChangedAttributes" => new ManagedReplacementTestData(
                CaseName: "ChangedAttributes",
                CenterlineCoordinates: [[0, 0, 0], [50, 0, 0]],
                ExistingWidth: 6,
                ExistingTransitType: (int)TransitType.Road,
                ReplacementWidth: 4,
                ReplacementTransitType: (int)TransitType.CyclePath,
                ExpectedCenterlineLayer: "AIQ Urban::Transit::CyclePath::Centerline::4"),

            _ => throw new ArgumentException($"Unknown test case: {caseName}")
        };
    }

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

    private static PolylineCurve SelfIntersectingCenterline()
    {
        return new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(10, 0, 0),
        ]);
    }
}
