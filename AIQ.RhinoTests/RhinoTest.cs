using ArchSitePlan.RhinoBhomInterop;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using System.Collections;
using System.Collections.Generic;

namespace AIQ.RhinoTests;

[RhinoTestFixture]
public class RhinoTest
{
    [Test]
    public void ConvertSurface()
    {
        var curves = new List<Curve>()
        {
            Curve.CreateControlPointCurve(
                new List<Point3d>()
                {
                    Point3d.Origin,
                    new Point3d(1,0,0),
                    new Point3d(1,1,0),
                    new Point3d(0,1,0),
                },
                1),
            (new Line(new Point3d(0,1,0), Point3d.Origin) ).ToNurbsCurve()
        };

        var brep = Brep.CreateEdgeSurface(curves);
        var bhom = brep.ToBHoMXYOutline();

        var isNull = bhom == null;
        Assert.Equals(isNull, false);
    }





    [Test]
    [TestCaseSource(nameof(PointList))]
    public void Test(Point3d point)
    {
        Assert.That(point.X, Is.GreaterThan(0));
        Assert.That(point.Y, Is.GreaterThan(0));
        Assert.That(point.Z, Is.GreaterThan(0));
    }

    public static IEnumerable PointList()
    {
        yield return new TestCaseData(new Point3d(1, 2, 3));
        yield return new TestCaseData(new Point3d(4, 5, 6));
        yield return new TestCaseData(new Point3d(7, 8, 9));
    }

}
