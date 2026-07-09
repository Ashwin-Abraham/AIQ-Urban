using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.ModelActions;

[RhinoTestFixture]
public class ResetUrbanElementsStepTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    public void Run_EmptyModel_ReturnsEmptyIdsAndModelStaysEmpty()
    {
        var step = new ResetUrbanElementsStep();
        var ids = ((IUrbanModelStep)step).Run(UrbanModel.Instance, []);

        Assert.That(ids, Is.Empty);
        Assert.That(UrbanModel.Instance.Transits, Is.Empty);
        Assert.That(UrbanModel.Instance.Buildings, Is.Empty);
        Assert.That(UrbanModel.Instance.Parcels, Is.Empty);
        Assert.That(UrbanModel.Instance.Zones, Is.Empty);
        Assert.That(UrbanModel.Instance.TransitSurfaces, Is.Empty);
        Assert.That(UrbanModel.Instance.ProjectMetrics, Is.Null);
    }

    [Test]
    public void Run_AllTypesCombined_ReturnsAllGeometryIdsAndModelIsEmpty()
    {
        // Arrange - one transit
        var centerlineId1 = Guid.NewGuid();
        var centerline1 = new Curve2D(
            new LineCurve(new Point3d(0, 0, 0), new Point3d(50, 0, 0)),
            centerlineId1);
        IUrbanModelStep addTransit = new AddTransitStep();
        addTransit.Run(UrbanModel.Instance, [new TransitData(centerline1, 6, TransitType.Road)]);

        // One building, 3 floors
        var footprint = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        IUrbanModelStep addBuilding = new AddBuildingStep();
        addBuilding.Run(UrbanModel.Instance, [new BuildingFootprintData(footprint, 3.5, 3, DefaultUses.Residential)]);

        // One parcel
        var parcelPolygon = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(100, 0, 0),
            new Point3d(100, 100, 0), new Point3d(0, 100, 0),
            new Point3d(0, 0, 0),
        ]));
        UrbanModel.Instance.Parcels.Add(
            new Parcel(parcelPolygon, DefaultUses.Public));

        // One zone
        var zonePolygon = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(100, 0, 0),
            new Point3d(100, 100, 0), new Point3d(0, 100, 0),
            new Point3d(0, 0, 0),
        ]));
        UrbanModel.Instance.AddZone(new Zone(zonePolygon, "TestZone"));

        // One transit surface
        var surfaceOutline = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(50, 0, 0),
            new Point3d(50, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var profile = new Profile2D(surfaceOutline, [])!;
        UrbanModel.Instance.TransitSurfaces.Add((profile, TransitType.Road));

        var expectedCount = 2 + 4 + 1 + 1 + 1; // 9

        // Act
        var step = new ResetUrbanElementsStep();
        var ids = ((IUrbanModelStep)step).Run(UrbanModel.Instance, []);

        // Assert
        Assert.That(ids, Has.Count.EqualTo(expectedCount));
        Assert.That(ids, Is.Unique);
        Assert.That(ids, Has.All.Not.EqualTo(Guid.Empty));

        Assert.That(UrbanModel.Instance.Transits, Is.Empty);
        Assert.That(UrbanModel.Instance.Buildings, Is.Empty);
        Assert.That(UrbanModel.Instance.Parcels, Is.Empty);
        Assert.That(UrbanModel.Instance.Zones, Is.Empty);
        Assert.That(UrbanModel.Instance.TransitSurfaces, Is.Empty);
        Assert.That(UrbanModel.Instance.ProjectMetrics, Is.Null);
    }

    [Test]
    public void Run_TransitSurfacesOnly_ReturnsSurfaceIdsAndModelIsEmpty()
    {
        // Arrange
        var outline1 = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(50, 0, 0),
            new Point3d(50, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var profile1 = new Profile2D(outline1, [])!;

        var outline2 = new Polygon2D(new PolylineCurve([
            new Point3d(60, 0, 0), new Point3d(100, 0, 0),
            new Point3d(100, 10, 0), new Point3d(60, 10, 0),
            new Point3d(60, 0, 0),
        ]));
        var profile2 = new Profile2D(outline2, [])!;

        UrbanModel.Instance.TransitSurfaces.Add((profile1, TransitType.Road));
        UrbanModel.Instance.TransitSurfaces.Add((profile2, TransitType.Railway));

        // Act
        var step = new ResetUrbanElementsStep();
        var ids = ((IUrbanModelStep)step).Run(UrbanModel.Instance, []);

        // Assert
        Assert.That(ids, Has.Count.EqualTo(2));
        Assert.That(ids, Does.Contain(profile1.Id));
        Assert.That(ids, Does.Contain(profile2.Id));

        Assert.That(UrbanModel.Instance.TransitSurfaces, Is.Empty);
        Assert.That(UrbanModel.Instance.Transits, Is.Empty);
        Assert.That(UrbanModel.Instance.Buildings, Is.Empty);
        Assert.That(UrbanModel.Instance.Parcels, Is.Empty);
        Assert.That(UrbanModel.Instance.Zones, Is.Empty);
        Assert.That(UrbanModel.Instance.ProjectMetrics, Is.Null);
    }
}
