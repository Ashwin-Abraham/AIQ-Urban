using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.DebugViz;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using AIQ.Urban.RhinoInterface.Tests.Helpers;
using Microsoft.Extensions.Logging;
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
    public void WithBuildingAddOrUpdateSteps_AddsBuildingsToModel(string testName)
    {
        // Arrange
        var testData = _testData.Single(x => x.CaseName == testName);

        using var doc = RhinoDoc.CreateHeadless(null);
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);

        // Act
        pipeline
            .WithDataGatherStep(new BuildingFootprintDataGatherStep(testData.Input))
            .WithBuildingAddOrUpdateSteps()
            .WithFullModelOutput()
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

    [Test]
    public void WithBuildingAddOrUpdateSteps_GatheredFootprint_DeletesInputCurve()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var footprint = new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(10, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]);
        var inputId = doc.Objects.AddCurve(footprint);
        var inputObject = doc.Objects.Find(inputId)!;
        var gatherStep = new SelectBuildingFootprintStep(
            [inputObject],
            3,
            2,
            DefaultUses.Create("Residential"));
        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(gatherStep)
            .WithBuildingAddOrUpdateSteps()
            .WithFullModelOutput()
            .Run();

        // Assert
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(1));
        Assert.That(doc.Objects.Find(inputId), Is.Null);
        Assert.That(doc.Objects.Count(), Is.EqualTo(4));

        var building = UrbanModel.Instance.Buildings.Single();
        Assert.That(doc.Objects.Find(building.Envelope.Id), Is.Not.Null);
        Assert.That(building.FloorPlates.Select(p => p.Outline.Id), Has.None.EqualTo(inputId));
    }

    [Test]
    public void WithBuildingAddOrUpdateSteps_GatheredEnvelope_DeletesInputBrep()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var envelope = Brep.CreateFromBox(new BoundingBox(
            new Point3d(0, 0, 0),
            new Point3d(10, 10, 6)));
        var inputId = doc.Objects.AddBrep(envelope);
        var inputObject = doc.Objects.Find(inputId)!;
        var gatherStep = new SelectBuildingEnvelopeStep(
            [inputObject],
            3,
            DefaultUses.Create("Commercial"),
            UrbanModel.Instance);

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(gatherStep)
            .WithBuildingAddOrUpdateSteps()
            .WithFullModelOutput()
            .Run();

        // Assert
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(1));
        Assert.That(doc.Objects.Find(inputId), Is.Null);
        Assert.That(doc.Objects.Count(), Is.EqualTo(4));

        var building = UrbanModel.Instance.Buildings.Single();
        Assert.That(building.Envelope.Id, Is.Not.EqualTo(inputId));
        Assert.That(doc.Objects.Find(building.Envelope.Id), Is.Not.Null);
    }

    [Test]
    public void WithBuildingAddOrUpdateSteps_ManagedAndUnmanagedEnvelopes_UpdatesAndAddsBuildings()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var residential = DefaultUses.Create("Residential");
        var commercial = DefaultUses.Create("Commercial");
        var undefined = DefaultUses.Create(DefaultUses.UndefinedName);

        var initialEnvelope = Brep.CreateFromBox(new BoundingBox(
            new Point3d(0, 0, 0),
            new Point3d(10, 10, 9)));
        var initialInputId = doc.Objects.AddBrep(initialEnvelope);
        var initialData = new BuildingMassData(
            new BrepElement(initialEnvelope, initialInputId),
            3,
            residential);

        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(new BuildingEnvelopeDataGatherStep([initialData], UrbanModel.Instance, [initialInputId]))
            .WithBuildingAddOrUpdateSteps()
            .WithFullModelOutput()
            .Run();

        var existingBuilding = UrbanModel.Instance.Buildings.Single();
        existingBuilding.FloorPlates[1].SetUse(commercial);
        var oldGeometryIds = existingBuilding.GetGeometryIds();

        var unmanagedEnvelope = Brep.CreateFromBox(new BoundingBox(
            new Point3d(20, 0, 0),
            new Point3d(30, 10, 9)));
        var unmanagedInputId = doc.Objects.AddBrep(unmanagedEnvelope);

        List<BuildingMassData> selectedEnvelopeData =
        [
            new BuildingMassData(
                new BrepElement(existingBuilding.Envelope.Geometry, existingBuilding.Envelope.Id),
                3,
                undefined),
            new BuildingMassData(
                new BrepElement(unmanagedEnvelope, unmanagedInputId),
                3,
                undefined),
        ];
        var selectedEnvelopeIds = selectedEnvelopeData
            .Select(data => data.Envelope.Id)
            .ToList();

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(new BuildingEnvelopeDataGatherStep(
                selectedEnvelopeData,
                UrbanModel.Instance,
                selectedEnvelopeIds))
            .WithBuildingAddOrUpdateSteps()
            .WithFullModelOutput()
            .Run();

        // Preview
#if DEBUG
        UrbanModel.Instance.VisualizeBuildings();
#endif

        // Assert
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(2));
        Assert.That(
            UrbanModel.Instance.Buildings.SelectMany(building => building.GetGeometryIds()),
            Has.None.Matches<Guid>(id => oldGeometryIds.Contains(id)));
        Assert.That(oldGeometryIds.Select(doc.Objects.Find), Has.All.Null);
        Assert.That(doc.Objects.Find(unmanagedInputId), Is.Null);

        var updatedBuilding = UrbanModel.Instance.Buildings
            .Single(building => building.FloorPlates.Any(plate => plate.Use.Name == commercial.Name));
        Assert.That(
            updatedBuilding.FloorPlates.OrderBy(plate => plate.Z()).Select(plate => plate.Use.Name),
            Is.EqualTo(new[] { residential.Name, commercial.Name, residential.Name }));

        var addedBuilding = UrbanModel.Instance.Buildings.Single(building => building != updatedBuilding);
        Assert.That(addedBuilding.FloorPlates.Select(plate => plate.Use.Name), Has.All.EqualTo(undefined.Name));
    }

    [Test]
    public void WithBuildingAddOrUpdateSteps_MixedSelfIntersectionInputs_LeavesRejectedCurveUntouched()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var validId = doc.Objects.AddCurve(FootprintCurve());
        var invalidId = doc.Objects.AddCurve(SelfIntersectingFootprintCurve());
        var selected = new[] { validId, invalidId }.Select(id => doc.Objects.FindId(id)!).ToList();
        var loggerProvider = new CapturingLoggerProvider();
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        var gatherStep = new SelectBuildingFootprintStep(
            selected,
            3,
            2,
            DefaultUses.Create("Residential"),
            loggerFactory);

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(gatherStep)
            .WithBuildingAddOrUpdateSteps()
            .WithFullModelOutput()
            .Run();

        // Assert
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(1));
        Assert.That(doc.Objects.FindId(validId), Is.Null);
        Assert.That(doc.Objects.FindId(invalidId), Is.Not.Null);
        var warnings = loggerProvider.Entries.Where(entry => entry.Level == LogLevel.Warning).ToList();
        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0].Message, Does.Contain(invalidId.ToString()));
    }

    [Test]
    public void WithBuildingAddOrUpdateSteps_AllInputsSelfIntersecting_CompletesWithNoMutations()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var invalidId = doc.Objects.AddCurve(SelfIntersectingFootprintCurve());
        var gatherStep = new SelectBuildingFootprintStep(
            [doc.Objects.FindId(invalidId)!],
            3,
            2,
            DefaultUses.Create("Residential"));

        // Act
        var mutationCount = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(gatherStep)
            .WithBuildingAddOrUpdateSteps()
            .WithFullModelOutput()
            .Run();

        // Assert
        Assert.That(mutationCount, Is.EqualTo(0));
        Assert.That(UrbanModel.Instance.Buildings, Is.Empty);
        Assert.That(doc.Objects.FindId(invalidId), Is.Not.Null);
    }

    private static PolylineCurve FootprintCurve()
    {
        return new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(10, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]);
    }

    private static PolylineCurve SelfIntersectingFootprintCurve()
    {
        return new PolylineCurve([
            new Point3d(20, 0, 0),
            new Point3d(30, 10, 0),
            new Point3d(20, 10, 0),
            new Point3d(30, 0, 0),
            new Point3d(20, 0, 0),
        ]);
    }

    private record BuildingPipelineTestData(
        string CaseName,
        List<BuildingFootprintData> Input,
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
            Input: [new BuildingFootprintData(Footprint1, 3.5, 5, DefaultUses.Create("Residential"))],
            ExpectedCount: 1),

        new BuildingPipelineTestData(
            CaseName: "MultipleBuildings",
            Input:
            [
                new BuildingFootprintData(Footprint1, 3.5, 5, DefaultUses.Create("Residential")),
                new BuildingFootprintData(Footprint2, 4.0, 3, DefaultUses.Create("Commercial")),
            ],
            ExpectedCount: 2),

        new BuildingPipelineTestData(
            CaseName: "EmptyList",
            Input: [],
            ExpectedCount: 0),
    ];
}
