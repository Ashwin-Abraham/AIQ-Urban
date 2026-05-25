using Rhino.Geometry;
using AIQ.Urban.Core.Extensions;

namespace AIQ.Urban.Core.Models.GeometryElements;

public class Polygon2D : IGeometryElement<Curve>
{
    public Curve Geometry { get; }
    public Guid Id { get; set; } = Guid.NewGuid();

    public Polygon2D(Curve geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        if (!geometry.IsPlanarInXY())
            throw new ArgumentException("Polygon2D requires a curve in an XY plane.", nameof(geometry));

        if (!geometry.IsClosed)
            throw new ArgumentException("Polygon2D requires a closed curve.", nameof(geometry));

        Geometry = geometry;
    }

    public static Polygon2D Create(double[][] coords)
    {
        ArgumentNullException.ThrowIfNull(coords);

        if (coords.Length < 3)
            throw new ArgumentException("At least 3 coordinate pairs are required.", nameof(coords));

        var points = coords.Select(c => new Point3d(c[0], c[1], 0)).ToList();

        var first = points[0];
        var last = points[^1];
        if (first.DistanceTo(last) > 1e-6)
            points.Add(first);

        var curve = new PolylineCurve(points);

        if (!curve.IsClosed)
            throw new ArgumentException("Coordinates must form a closed ring.");

        return new Polygon2D(curve);
    }

    internal double Area()
    {
        return AreaMassProperties.Compute(Geometry)!.Area;
    }

    internal double Perimeter()
    {
        return Geometry.GetLength();
    }

    public Brep ToBrep(double tolerance = 1e-5)
    {
        return Brep.CreatePlanarBreps(Geometry, tolerance).FirstOrDefault()!;
    }
}
