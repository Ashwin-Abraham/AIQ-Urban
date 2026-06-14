using Rhino.Geometry;
using AIQ.Urban.Core.Extensions;

namespace AIQ.Urban.Core.Models.GeometryElements;

public class Polygon2D : IGeometryElement<PolylineCurve>
{
    public PolylineCurve Geometry { get; }
    public Guid Id { get; set; } = Guid.NewGuid();

    public Polygon2D(Curve geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        var copy = geometry.Duplicate() as PolylineCurve
            ?? throw new InvalidOperationException("Failed to duplicate curve.");

        if (!copy.IsPlanarInXY())
            throw new ArgumentException("Polygon2D requires a curve in an XY plane.", nameof(geometry));

        if (!copy.IsClosed)
            throw new ArgumentException("Polygon2D requires a closed curve.", nameof(geometry));

        Geometry = copy;
    }

    public static Polygon2D Create(List<(double X, double Y)> coordinates)
    {
        ArgumentNullException.ThrowIfNull(coordinates);

        if (coordinates.Count < 3)
            throw new ArgumentException("At least 3 coordinate pairs are required.", nameof(coordinates));

        var points = coordinates.Select(c => new Point3d(c.X, c.Y, 0)).ToList();

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
