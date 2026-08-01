using System;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Models;

[RhinoTestFixture]
public class TransitTests
{
    [Test]
    public void Constructor_CenterlineOutsideGroundTolerance_ThrowsArgumentException()
    {
        var curve = new Curve2D(
            new LineCurve(new Point3d(0, 0, 1), new Point3d(10, 0, 1)),
            Guid.NewGuid());

        var exception = Assert.Throws<ArgumentException>(
            () => new Transit(curve, 6, TransitType.Road));

        Assert.That(exception!.Message, Does.Contain("Z = 0"));
    }

    [Test]
    public void TransitData_CenterlineWithinTolerance_NormalizesToWorldXY()
    {
        var curve = new Curve2D(
            new LineCurve(new Point3d(0, 0, 0.000009), new Point3d(10, 0, 0.000009)),
            Guid.NewGuid());

        var result = new TransitData(curve, 6, TransitType.Road);

        var bounds = result.Centerline.Geometry.GetBoundingBox(true);
        Assert.That(bounds.Min.Z, Is.EqualTo(0));
        Assert.That(bounds.Max.Z, Is.EqualTo(0));
    }

    [Test]
    public void TransitUpdateData_CenterlineOutsideGroundTolerance_ThrowsArgumentException()
    {
        var curve = new LineCurve(
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0.001));

        var exception = Assert.Throws<ArgumentException>(
            () => new TransitUpdateData(Guid.NewGuid(), curve));

        Assert.That(exception!.Message, Does.Contain("Z = 0"));
    }
}
