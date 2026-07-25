using System;
using System.Linq;
using System.Text.Json;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.Metrics;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.RhinoInterface.Tests;

[RhinoTestFixture]
public class SelectionMetricsMessageTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    public void FromSelection_EmptySelection_ReturnsThreeEmptyAggregates()
    {
        // Arrange
        var model = UrbanModel.Instance;
        ProjectMetricsCalculator.CalculateAndStore(model, containmentTolerance: 0.001);

        // Act
        var message = SelectionMetricsMessage.FromSelection(model, []);

        // Assert
        Assert.That(message.Buildings.Count, Is.Zero);
        Assert.That(message.Parcels.Count, Is.Zero);
        Assert.That(message.Transits.Count, Is.Zero);
    }

    [Test]
    public void FromSelection_MixedSelection_AggregatesAllSupportedTypes()
    {
        // Arrange
        var model = UrbanModel.Instance;
        var use = model.GetUse("Residential");
        var building = CreateBuilding(2, 2, 2, use);
        var parcel = CreateParcel(0, 0, 10, use);
        var transit = CreateTransit(0, 5, 10, 5, width: 4);
        model.Buildings.Add(building);
        model.Parcels.Add(parcel);
        model.Transits.Add(transit);
        ProjectMetricsCalculator.CalculateAndStore(model, containmentTolerance: 0.001);

        // Act
        var message = SelectionMetricsMessage.FromSelection(model, [
            building.Envelope.Id,
            parcel.Outline.Id,
            transit.Centerline.Id,
        ]);

        // Assert
        Assert.That(message.Buildings.Count, Is.EqualTo(1));
        Assert.That(message.Parcels.Count, Is.EqualTo(1));
        Assert.That(message.Transits.Count, Is.EqualTo(1));
        Assert.That(message.Buildings.BuiltUpArea, Is.GreaterThan(0));
        Assert.That(message.Parcels.LandArea, Is.GreaterThan(0));
        Assert.That(message.Transits.RunningLength, Is.EqualTo(10).Within(1e-6));
    }

    [Test]
    public void FromSelection_BuildingEnvelopeAndFloorPlate_CountsBuildingOnce()
    {
        // Arrange
        var model = UrbanModel.Instance;
        var building = CreateBuilding(0, 0, 5, model.GetUse("Residential"));
        model.Buildings.Add(building);
        ProjectMetricsCalculator.CalculateAndStore(model, containmentTolerance: 0.001);

        // Act
        var message = SelectionMetricsMessage.FromSelection(model, [
            building.Envelope.Id,
            building.FloorPlates[0].Outline.Id,
        ]);

        // Assert
        Assert.That(message.Buildings.Count, Is.EqualTo(1));
    }

    [Test]
    public void FromSelection_FloorPlateOnly_ResolvesOwningBuilding()
    {
        // Arrange
        var model = UrbanModel.Instance;
        var building = CreateBuilding(0, 0, 5, model.GetUse("Residential"));
        model.Buildings.Add(building);
        ProjectMetricsCalculator.CalculateAndStore(model, containmentTolerance: 0.001);

        // Act
        var message = SelectionMetricsMessage.FromSelection(
            model,
            [building.FloorPlates[0].Outline.Id]);

        // Assert
        Assert.That(message.Buildings.Count, Is.EqualTo(1));
    }

    [Test]
    public void FromSelection_UnsupportedObjectId_IsIgnored()
    {
        // Arrange
        var model = UrbanModel.Instance;
        ProjectMetricsCalculator.CalculateAndStore(model, containmentTolerance: 0.001);

        // Act
        var message = SelectionMetricsMessage.FromSelection(model, [Guid.NewGuid()]);

        // Assert
        Assert.That(message.Buildings.Count, Is.Zero);
        Assert.That(message.Parcels.Count, Is.Zero);
        Assert.That(message.Transits.Count, Is.Zero);
    }

    [Test]
    public void FromSelection_MultipleParcels_CalculatesRatiosFromCombinedTotals()
    {
        // Arrange
        var model = UrbanModel.Instance;
        var use = model.GetUse("Residential");
        var firstParcel = CreateParcel(0, 0, 10, use);
        var secondParcel = CreateParcel(20, 0, 20, use);
        model.Parcels.AddRange([firstParcel, secondParcel]);
        model.Buildings.Add(CreateBuilding(2, 2, 2, use));
        model.Buildings.Add(CreateBuilding(25, 5, 5, use));
        ProjectMetricsCalculator.CalculateAndStore(model, containmentTolerance: 0.001);

        var selectedMetrics = model.ProjectMetrics!.ParcelMetrics;
        var expectedLandArea = selectedMetrics.Sum(metrics => metrics.LandArea);
        var expectedBuiltUpArea = selectedMetrics.Sum(metrics => metrics.BuiltUpArea);
        var expectedBuiltFootprint = selectedMetrics.Sum(metrics => metrics.BuiltFootprint);

        // Act
        var message = SelectionMetricsMessage.FromSelection(model, [
            firstParcel.Outline.Id,
            secondParcel.Outline.Id,
        ]);

        // Assert
        Assert.That(message.Parcels.FAR, Is.EqualTo(expectedBuiltUpArea / expectedLandArea).Within(1e-6));
        Assert.That(
            message.Parcels.CoveragePercentage,
            Is.EqualTo(expectedBuiltFootprint / expectedLandArea * 100).Within(1e-6));
    }

    [Test]
    public void Serialize_Message_UsesNewTypedContract()
    {
        // Act
        var json = JsonSerializer.Serialize(SelectionMetricsMessage.Empty);

        // Assert
        Assert.That(json, Does.Contain("\"Type\":\"SelectionMetricsMessage\""));
        Assert.That(json, Does.Contain("\"Buildings\""));
        Assert.That(json, Does.Contain("\"Parcels\""));
        Assert.That(json, Does.Contain("\"Transits\""));
        Assert.That(json, Does.Not.Contain("\"MinimumHeight\""));
        Assert.That(json, Does.Not.Contain("\"MaximumHeight\""));
        Assert.That(json, Does.Not.Contain("\"AverageHeight\""));
        Assert.That(json, Does.Not.Contain("\"AreaByUse\""));
        Assert.That(json, Does.Not.Contain("\"LandAreaByUse\""));
        Assert.That(json, Does.Not.Contain("\"Breakdown\""));
        Assert.That(json, Does.Not.Contain("SelectedObjectMetricsMessage"));
    }

    [Test]
    public void ModelStateMessage_TransitsShareTypeAndWidth_GroupsChartData()
    {
        // Arrange
        var model = UrbanModel.Instance;
        model.Transits.AddRange([
            CreateTransit(0, 0, 10, 0, width: 6),
            CreateTransit(0, 10, 20, 10, width: 6),
        ]);
        ProjectMetricsCalculator.CalculateAndStore(model, containmentTolerance: 0.001);

        // Act
        var message = new ModelStateMessage(model);
        var all = message.StudyAreaDataForUI.Single(item => item.StudyName == "All");

        // Assert
        Assert.That(all.RoadLengthData, Has.Count.EqualTo(1));
        Assert.That(all.RoadLengthData[0].Data, Is.EqualTo(30).Within(1e-6));
        Assert.That(all.RoadAreaData, Has.Count.EqualTo(1));
        Assert.That(all.RoadAreaData[0].Data, Is.EqualTo(180).Within(1e-6));
    }

    private static Building CreateBuilding(
        double x,
        double y,
        double size,
        Use use)
    {
        return new Building(
            CreatePolygon(x, y, size),
            typicalFloorHeight: 3,
            floorCount: 2,
            use);
    }

    private static Parcel CreateParcel(
        double x,
        double y,
        double size,
        Use use)
    {
        return new Parcel(
            new PolyCurve2D(CreatePolygon(x, y, size).Geometry),
            use);
    }

    private static Transit CreateTransit(
        double fromX,
        double fromY,
        double toX,
        double toY,
        double width)
    {
        var centerline = new Curve2D(
            new LineCurve(
                new Point3d(fromX, fromY, 0),
                new Point3d(toX, toY, 0)),
            Guid.NewGuid());

        return new Transit(centerline, width, TransitType.Road);
    }

    private static Polygon2D CreatePolygon(
        double x,
        double y,
        double size)
    {
        return new Polygon2D(new PolylineCurve([
            new Point3d(x, y, 0),
            new Point3d(x + size, y, 0),
            new Point3d(x + size, y + size, 0),
            new Point3d(x, y + size, 0),
            new Point3d(x, y, 0),
        ]));
    }
}
