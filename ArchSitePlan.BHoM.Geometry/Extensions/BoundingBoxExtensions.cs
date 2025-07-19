using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.Engine.Geometry;
using BH.oM.Data.Collections;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Extensions;
public static class BoundingBoxExtensions
{
    public static List<Point> FourPoints(this BoundingBox boundingBox)
    {
        var min = boundingBox.Min;
        var max = boundingBox.Max;

        var p1 = Create.Point(min.X, min.Y); // bottom-left
        var p2 = Create.Point(max.X, min.Y); // bottom-right
        var p3 = Create.Point(max.X, max.Y); // top-right
        var p4 = Create.Point(min.X, max.Y); // top-left

        return new List<Point> { p1, p2, p3, p4 };
    }
    public static Point PointAt(this BoundingBox box,double u, double v, double w)
    {
        var xRange = new Domain(box.Min.X, box.Max.X);
        var yRange = new Domain(box.Min.Y, box.Max.Y);
        var zRange = new Domain(box.Min.Z, box.Max.Z);
        var domain0to1 = new Domain(0, 1);

        var realWorldX = domain0to1.Remap(u, xRange);
        var realWorldY = domain0to1.Remap(u, yRange);
        var realWorldZ = domain0to1.Remap(u, zRange);

        return Create.Point(realWorldX, realWorldY, realWorldZ);
    }

    public static Domain XDomain(this BoundingBox box)
    {
        return new(box.Min.X, box.Max.X);
    }

    public static Domain YDomain(this BoundingBox box)
    {
        return new(box.Min.Y, box.Max.Y);
    }

    public static Domain ZDomain(this BoundingBox box)
    {
        return new(box.Min.Z, box.Max.Z);
    }
}
