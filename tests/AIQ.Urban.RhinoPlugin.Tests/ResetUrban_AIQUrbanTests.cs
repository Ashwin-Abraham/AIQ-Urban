using System.Collections.Generic;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.RhinoInterface.Tests.Helpers;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using AIQ.Urban.RhinoInterface.Utils;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.RhinoInterface.Tests;

[RhinoTestFixture]
public class ResetUrban_AIQUrbanTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    public void Run_WithExistingBuilding_DeletesAllRhinoObjects()
    {
        using var doc = RhinoDoc.CreateHeadless(null);

        // Arrange: add a building via the pipeline (bakes to Rhino doc)
        var footprint = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var buildingData = new BuildingFootprintData(footprint, 3.5, 3, DefaultUses.Create("Residential"));
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(new BuildingFootprintDataGatherStep([buildingData]))
            .WithBuildingAddOrUpdateSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation(false);

        pipeline.Run();

        // Verify model and Rhino doc have the geometry
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(1));
        RhinoDocAssertions.AssertLayerObjectCount(doc, LayerConstants.BuildingEnvelope, 1);
        RhinoDocAssertions.AssertLayerObjectCount(doc, LayerConstants.BuildingFloorPlate(DefaultUses.Create("Residential")), 3);

        // Act: reset
        pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
        pipeline.WithUrbanElementStep(new ResetUrbanElementsStep());
        pipeline.WithFullModelOutput();
        pipeline.Run();

        // Assert: model is empty
        Assert.That(UrbanModel.Instance.Buildings, Is.Empty);
        Assert.That(UrbanModel.Instance.Transits, Is.Empty);
        Assert.That(UrbanModel.Instance.Parcels, Is.Empty);
        Assert.That(UrbanModel.Instance.Zones, Is.Empty);
        Assert.That(UrbanModel.Instance.TransitSurfaces, Is.Empty);
        Assert.That(UrbanModel.Instance.ProjectMetrics, Is.Null);

        // Assert: Rhino doc has no objects on AIQ Urban layers
        RhinoDocAssertions.AssertLayerObjectCount(doc, LayerConstants.BuildingEnvelope, 0);
        RhinoDocAssertions.AssertLayerObjectCount(doc, LayerConstants.BuildingFloorPlate(DefaultUses.Create("Residential")), 0);
    }
}
