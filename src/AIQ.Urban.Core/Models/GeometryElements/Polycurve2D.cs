using Rhino.Geometry;
using AIQ.Urban.Core.Extensions;

namespace AIQ.Urban.Core.Models.GeometryElements;

public class Polycurve2D : IGeometryElement<PolylineCurve>
{
    public PolylineCurve Geometry { get; }
    public Guid Id { get; set; } = Guid.NewGuid();

    public Polycurve2D(Curve geometry)
    {
        PolylineCurve? plineCrv = null;
        if (geometry is PolylineCurve)
            plineCrv = (PolylineCurve)geometry;
        else if (geometry.TryGetPolyline(out Polyline pline))
            plineCrv = pline.ToPolylineCurve();

        if(plineCrv is null)
            throw new InvalidOperationException("Making a polygon2D is only allowed with a polyline curve. Received a " + geometry.GetType());

        var copy = (plineCrv.Duplicate() as PolylineCurve)!;

        if (!copy.IsPlanarInXY())
            throw new ArgumentException("Polygon2D requires a curve in an XY plane.", nameof(geometry));

        if (!copy.IsClosed)
            throw new ArgumentException("Polygon2D requires a closed curve.", nameof(geometry));

        Geometry = copy;
    }

    public static Polycurve2D Rectangle(Point2d min, Point2d max)
    {
        return Create([
            (min.X, min.Y),
            (max.X, min.Y),
            (max.X, max.Y),
            (min.X, max.Y),
            ]);
    }

    public static Polycurve2D? Create(Curve curve)
    {
        if(curve.Degree > 1)
            return null;

        if (curve is not PolylineCurve pline)
            return null;

        var points = new List<(double X, double Y)>();

        for(int i = 0; i < pline.PointCount; i++)
        {
            var point = pline.PointAt(i);
            points.Add((point.X, point.Y));
        }

        return Create(points);
    }

    public static Polycurve2D? Create(List<(double X, double Y)> coordinates)
    {
        if (coordinates.Count < 3)
            return null;

        var points = coordinates.Select(c => new Point3d(c.X, c.Y, 0)).ToList();

        var first = points[0];
        var last = points[^1];
        if (first.DistanceTo(last) > 1e-6)
            points.Add(first);

        var curve = new PolylineCurve(points);

        if (!curve.IsClosed)
            return null;

        return new Polycurve2D(curve);
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
