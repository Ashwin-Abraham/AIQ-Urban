using System;
using AIQ.Urban.Core.Extensions;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Extensions;

[RhinoTestFixture]
public class TransitExtensionsTests
{
    [Test]
    [TestCase("ExactGround", 0, 0, true)]
    [TestCase("WithinTolerance", -0.000009, 0.000009, true)]
    [TestCase("AboveTolerance", 0, 0.000011, false)]
    [TestCase("BelowTolerance", -0.000011, 0, false)]
    public void IsOnTransitGroundPlane_ZRange_ReturnsExpected(
        string caseName,
        double startZ,
        double endZ,
        bool expected)
    {
        var curve = new LineCurve(
            new Point3d(0, 0, startZ),
            new Point3d(10, 0, endZ));

        var result = curve.IsOnTransitGroundPlane();

        Assert.That(result, Is.EqualTo(expected), caseName);
    }

    [Test]
    public void NormalizeTransitCenterline_WithinTolerance_ProjectsEntireCurveToWorldXY()
    {
        var curve = new LineCurve(
            new Point3d(0, 0, -0.000009),
            new Point3d(10, 0, 0.000009));

        var result = curve.NormalizeTransitCenterline();

        var bounds = result.GetBoundingBox(true);
        Assert.That(bounds.Min.Z, Is.EqualTo(0));
        Assert.That(bounds.Max.Z, Is.EqualTo(0));
    }

    [Test]
    public void NormalizeTransitCenterline_OutsideTolerance_ThrowsArgumentException()
    {
        var curve = new LineCurve(
            new Point3d(0, 0, 0),
            new Point3d(10, 0, 0.001));

        var exception = Assert.Throws<ArgumentException>(() => curve.NormalizeTransitCenterline());

        Assert.That(exception!.Message, Does.Contain("Z = 0"));
    }
}
