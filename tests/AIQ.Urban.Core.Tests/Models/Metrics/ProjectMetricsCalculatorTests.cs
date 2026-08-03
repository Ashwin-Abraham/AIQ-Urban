using System;
using System.Linq;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.Metrics;
using AIQ.Urban.Core.Models.UrbanElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Models.Metrics;

[RhinoTestFixture]
public class ProjectMetricsCalculatorTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    public void CalculateAndStore_ModelElements_AssignsSourceElementIds()
    {
        // Arrange
        var model = UrbanModel.Instance;
        var use = model.GetUse("Residential");
        var building = CreateBuilding(2, 2, 2, use);
        var parcel = CreateParcel(0, 0, 10, use);
        var transit = CreateTransit(0, 5, 10, 5, width: 4);
        var zone = new Zone(CreatePolygon(-1, -1, 12), "Test Zone");

        model.Buildings.Add(building);
        model.Parcels.Add(parcel);
        model.Transits.Add(transit);
        model.Zones.Add(zone);

        // Act
        ProjectMetricsCalculator.CalculateAndStore(model, containmentTolerance: 0.001);

        // Assert
        Assert.That(model.ProjectMetrics, Is.Not.Null);
        Assert.That(model.ProjectMetrics!.BuildingMetrics.Single().SourceElementId, Is.EqualTo(building.Id));
        Assert.That(model.ProjectMetrics.ParcelMetrics.Single().SourceElementId, Is.EqualTo(parcel.Id));
        Assert.That(model.ProjectMetrics.TransitMetrics.Single().SourceElementId, Is.EqualTo(transit.Id));
        Assert.That(model.ProjectMetrics.ZoneMetrics.Single().SourceElementId, Is.EqualTo(zone.Id));
    }

    [Test]
    public void CalculateAndStore_TransitsShareTypeAndWidth_RetainsPerTransitMetrics()
    {
        // Arrange
        var model = UrbanModel.Instance;
        var first = CreateTransit(0, 0, 10, 0, width: 6);
        var second = CreateTransit(0, 10, 20, 10, width: 6);
        model.Transits.AddRange([first, second]);

        // Act
        ProjectMetricsCalculator.CalculateAndStore(model, containmentTolerance: 0.001);

        // Assert
        Assert.That(model.ProjectMetrics, Is.Not.Null);
        Assert.That(model.ProjectMetrics!.TransitMetrics, Has.Count.EqualTo(2));
        Assert.That(
            model.ProjectMetrics.TransitMetrics.Select(metrics => metrics.SourceElementId),
            Is.EquivalentTo(new[] { first.Id, second.Id }));
        Assert.That(model.ProjectMetrics.TotalTransitLength, Is.EqualTo(30).Within(1e-6));
        Assert.That(model.ProjectMetrics.TotalTransitArea, Is.EqualTo(180).Within(1e-6));
    }

    [Test]
    public void CalculateAndStore_Zone_RetainsBoundaryAreaAndAssignedParcelMetrics()
    {
        // Arrange
        var model = UrbanModel.Instance;
        var use = model.GetUse("Residential");
        var parcel = CreateParcel(0, 0, 10, use);
        var zone = new Zone(CreatePolygon(-1, -1, 12), "Test Zone");
        model.Parcels.Add(parcel);
        model.Zones.Add(zone);

        // Act
        ProjectMetricsCalculator.CalculateAndStore(model, containmentTolerance: 0.001);

        // Preview
        var zoneMetrics = model.ProjectMetrics!.ZoneMetrics.Single();
        TestContext.WriteLine(
            $"Boundary: {zoneMetrics.ZoneBoundaryArea}; parcels: {zoneMetrics.ParcelMetrics.Count}");

        // Assert
        Assert.That(zoneMetrics.ZoneBoundaryArea, Is.EqualTo(144).Within(1e-6));
        Assert.That(zoneMetrics.ParcelMetrics, Has.Count.EqualTo(1));
        Assert.That(zoneMetrics.ParcelMetrics.Single().SourceElementId, Is.EqualTo(parcel.Id));
    }

    [Test]
    public void Constructor_SyntheticAllZone_UsesEmptySourceElementId()
    {
        // Act
        var metrics = new ZoneMetrics(
            Guid.Empty,
            "All",
            zoneBoundaryArea: 0,
            buildings: [],
            parcels: [],
            transits: []);

        // Assert
        Assert.That(metrics.SourceElementId, Is.EqualTo(Guid.Empty));
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
