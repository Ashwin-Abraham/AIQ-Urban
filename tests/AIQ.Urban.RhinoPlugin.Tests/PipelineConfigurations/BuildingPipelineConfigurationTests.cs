using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.DebugViz;
using AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;
using AIQ.Urban.RhinoInterface.Tests.Helpers;
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
        var pipeline = new RhinoUrbanModelPipeline(
            doc,
            UrbanModel.Instance,
            new PreparedInputPreparationStep(testData.Input));

        // Act
        pipeline
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
        var preparationStep = new AddOrUpdateBuildingInputPreparationStep(
            [inputId],
            3,
            2,
            "Residential",
            BuildingInputMode.Footprint);

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, preparationStep)
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
        var preparationStep = new AddOrUpdateBuildingInputPreparationStep(
            [inputId],
            3,
            0,
            "Commercial",
            BuildingInputMode.Mass);

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, preparationStep)
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

        new RhinoUrbanModelPipeline(
                doc,
                UrbanModel.Instance,
                new PreparedInputPreparationStep([
                    initialData,
                    new RhinoObjectDeletionData(initialInputId),
                ]))
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

        var preparationStep = new AddOrUpdateBuildingInputPreparationStep(
            [existingBuilding.Envelope.Id, unmanagedInputId],
            3,
            0,
            undefined.Name,
            BuildingInputMode.Mass);

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, preparationStep)
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
