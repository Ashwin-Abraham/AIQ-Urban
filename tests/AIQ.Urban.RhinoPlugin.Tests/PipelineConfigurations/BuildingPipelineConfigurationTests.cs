using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.DebugViz;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
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
            .WithBuildingAdditionSteps(testData.Input.Cast<ICreationData>().ToList(), [])
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
    public void WithBuildingAdditionSteps_GatheredFootprint_DeletesInputCurve()
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
        var creationData = gatherStep.Run(doc);
        var idsToDelete = creationData
            .OfType<BuildingFootprintData>()
            .Select(data => data.Footprint.Id)
            .ToList();

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(gatherStep)
            .WithBuildingAdditionSteps([], idsToDelete)
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
    public void WithBuildingAdditionSteps_GatheredEnvelope_DeletesInputBrep()
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
            DefaultUses.Create("Commercial"));
        var creationData = gatherStep.Run(doc);
        var idsToDelete = creationData
            .OfType<BuildingMassData>()
            .Select(data => data.Envelope.Id)
            .ToList();

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithCreationData(creationData)
            .WithBuildingAdditionSteps([], idsToDelete)
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
