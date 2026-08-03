using System.Collections.Generic;
using System.Drawing;
using System;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.Metrics;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Models.UrbanGraph;
using AIQ.Urban.RhinoInterface.Tests.Helpers;
using AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;
using AIQ.Urban.RhinoInterface.Utils;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.RhinoInterface.Tests;

[RhinoTestFixture]
public class DeleteAllUrbanElements_AIQUrbanTests
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
        var pipeline = new RhinoUrbanModelPipeline(
                doc,
                UrbanModel.Instance,
                new PreparedInputPreparationStep([buildingData]))
            .WithBuildingAddOrUpdateSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation(false);

        pipeline.Run();

        // Verify model and Rhino doc have the geometry
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(1));
        RhinoDocAssertions.AssertLayerObjectCount(doc, LayerConstants.BuildingEnvelope, 1);
        RhinoDocAssertions.AssertLayerObjectCount(doc, LayerConstants.BuildingFloorPlate(DefaultUses.Create("Residential")), 3);

        // Act: reset
        new RhinoUrbanModelPipeline(
                doc,
                UrbanModel.Instance,
                new DeleteAllUrbanElementsInputPreparationStep())
            .WithDeleteAllUrbanElementsSteps()
            .WithFullModelOutput()
            .Run();

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

    [Test]
    public void DeleteAllUrbanElements_WithCustomUse_PreservesUseAndDeletesModeledContent()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var customUse = new Use(Guid.NewGuid(), "Custom", Color.CornflowerBlue);
        UrbanModel.Instance.Uses.Add(customUse);
        var footprint = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        new AddBuildingStep().Run(
            UrbanModel.Instance,
            [new BuildingFootprintData(footprint, 3.5, 3, customUse)]);
        var preparationStep = new DeleteAllUrbanElementsInputPreparationStep();

        // Act
        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, preparationStep)
            .WithDeleteAllUrbanElementsSteps()
            .Run();

        // Assert
        Assert.That(UrbanModel.Instance.GetUrbanElements(), Is.Empty);
        Assert.That(UrbanModel.Instance.Uses, Does.Contain(customUse));
    }

    [Test]
    public void DeleteAllUrbanElements_WithAllElementTypes_ClearsRootAndDerivedState()
    {
        using var doc = RhinoDoc.CreateHeadless(null);
        var model = UrbanModel.Instance;
        var publicUse = model.GetUse("Public");
        var footprint = new Polygon2D(Rectangle(0, 0, 10, 10));
        var zoneBoundary = new Polygon2D(Rectangle(-10, -10, 30, 30));
        var parcelOutline = new Polygon2D(Rectangle(-5, -5, 20, 20));
        var transitSurfaceOutline = new Polygon2D(Rectangle(-2, -2, 15, 2));
        var transitSurface = new Profile2D(transitSurfaceOutline, [])!;

        new AddTransitStep().Run(model, [new TransitData(
            new Curve2D(
                new LineCurve(new Point3d(-5, 0, 0), new Point3d(20, 0, 0)),
                Guid.NewGuid()),
            6,
            TransitType.Road)]);
        new AddBuildingStep().Run(model, [new BuildingFootprintData(footprint, 3, 2, publicUse)]);
        new AddZoneStep().Run(model, [new ZoneData(zoneBoundary, "Study Area")]);
        new AddParcelBoundaryStep().Run(model, [new ParcelBoundaryData(
            new Curve2D(
                new LineCurve(new Point3d(5, -5, 0), new Point3d(5, 20, 0)),
                Guid.NewGuid()))]);
        model.Parcels.Add(new Parcel(parcelOutline.ToPolyCurve2d(), publicUse));
        model.TransitSurfaces.Add((transitSurface, TransitType.Road));
        model.UrbanGraph = new Graph([new GraphNode(Point3d.Origin, [])], []);
        ProjectMetricsCalculator.CalculateAndStore(model, 0.001);

        new RhinoUrbanModelPipeline(
                doc,
                model,
                new DeleteAllUrbanElementsInputPreparationStep())
            .WithDeleteAllUrbanElementsSteps()
            .Run();

        Assert.That(model.GetUrbanElements(), Is.Empty);
        Assert.That(model.TransitSurfaces, Is.Empty);
        Assert.That(model.UrbanGraph.Nodes, Is.Empty);
        Assert.That(model.UrbanGraph.Edges, Is.Empty);
        Assert.That(model.ProjectMetrics, Is.Null);
    }

    private static PolylineCurve Rectangle(double minX, double minY, double maxX, double maxY)
    {
        return new PolylineCurve([
            new Point3d(minX, minY, 0),
            new Point3d(maxX, minY, 0),
            new Point3d(maxX, maxY, 0),
            new Point3d(minX, maxY, 0),
            new Point3d(minX, minY, 0),
        ]);
    }
}
