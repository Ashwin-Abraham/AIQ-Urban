using Rhino.Geometry;

namespace AIQ.Urban.Core.Extensions;

public static class PolylineExtensions
{
    public static Polyline? CreateMinimalBox(this Polyline curve)
    {
        var points = curve.ToArray();
        if (points.Length < 3)
            return null;

        var pts2d = points.Select(p => new Point2d(p.X, p.Y)).Distinct().ToArray();
        if (pts2d.Length < 3)
            return null;

        var hull = pts2d.GrahamConvexHull();
        if (hull.Count < 3)
            return null;

        var pointsList = hull.Select(p => new Point3d(p.X, p.Y, 0)).ToList();
        (BoundingBox Box, Transform Rotator) = ComputeMinAreaOrientedBox(pointsList);

        var pline = Box.ToPolylineXY();

        if (!Rotator.TryGetInverse(out var inverse))
            return null;
        
        pline.Transform(inverse);
        return new Polyline(pline);
    }

    private static (BoundingBox, Transform) ComputeMinAreaOrientedBox(List<Point3d> hullPoints)
    {
        (BoundingBox Box, double Area, Transform BoxRotation) minResult = (BoundingBox.Empty, double.MaxValue, Transform.Identity);
        for(int i = 0; i < hullPoints.Count; i++)
        {
            var start = hullPoints[i];
            var end = hullPoints[(i + 1) % hullPoints.Count];
            var direction = end.ToVector3d() - start.ToVector3d();

            var rotator = Transform.Rotation(direction, Vector3d.XAxis, start);
            var box = new BoundingBox(hullPoints, rotator);

            if (box.Area < minResult.Area)
                minResult = (box, box.Area, rotator);
        }

        return (minResult.Box, minResult.BoxRotation);
    }
}
