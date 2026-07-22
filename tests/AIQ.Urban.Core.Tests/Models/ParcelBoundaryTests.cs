using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using System;

namespace AIQ.Urban.Core.Tests.Models;

[RhinoTestFixture]
public class ParcelBoundaryTests
{
    [Test]
    public void Constructor_SetsCurveProperty()
    {
        // Arrange
        var curve = new Curve2D(new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0)), Guid.NewGuid());

        // Act
        var boundary = new ParcelBoundary(curve);

        // Assert
        Assert.That(boundary.Curve, Is.SameAs(curve));
    }

    [Test]
    public void Id_ReturnsCurveId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var curve = new Curve2D(new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0)), id);

        // Act
        var boundary = new ParcelBoundary(curve);

        // Assert
        Assert.That(boundary.Id, Is.EqualTo(id));
    }

    [Test]
    public void GetRepresentativeCenter_ReturnsMidpoint()
    {
        // Arrange
        var curve = new Curve2D(new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0)), Guid.NewGuid());
        var boundary = new ParcelBoundary(curve);

        // Act
        var center = boundary.GetRepresentativeCenter();

        // Assert
        Assert.That(center.X, Is.EqualTo(5.0).Within(1e-6));
        Assert.That(center.Y, Is.EqualTo(0.0).Within(1e-6));
        Assert.That(center.Z, Is.EqualTo(0.0).Within(1e-6));
    }

    [Test]
    public void GetGeometryIds_ReturnsSingleCurveId()
    {
        // Arrange
        var curve = new Curve2D(new LineCurve(new Point3d(0, 0, 0), new Point3d(10, 0, 0)), Guid.NewGuid());
        var boundary = new ParcelBoundary(curve);

        // Act
        var ids = boundary.GetGeometryIds();

        // Assert
        Assert.That(ids, Has.Count.EqualTo(1));
        Assert.That(ids[0], Is.EqualTo(curve.Id));
    }

    [Test]
    public void GetRepresentativeCenter_ClosedCurve_ReturnsMidpoint()
    {
        // Arrange — closed rectangle (0,0)-(10,0)-(10,10)-(0,10), perimeter = 40
        // Normalized length 0.5 = distance 20 along perimeter = corner (10,10)
        var curve = new Curve2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0),
            new Point3d(10, 10, 0),
            new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]), Guid.NewGuid());
        var boundary = new ParcelBoundary(curve);

        // Act
        var center = boundary.GetRepresentativeCenter();

        // Assert — midpoint of perimeter at 0.5 lands at corner (10,10)
        Assert.That(center.X, Is.EqualTo(10.0).Within(1e-6));
        Assert.That(center.Y, Is.EqualTo(10.0).Within(1e-6));
    }
}
