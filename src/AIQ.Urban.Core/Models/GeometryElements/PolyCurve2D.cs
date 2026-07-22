using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.GeometryElements;

public class PolyCurve2D : IGeometryElement<PolyCurve>
{
    public PolyCurve Geometry { get; }
    public Guid Id { get; set; } = Guid.NewGuid();

    public PolyCurve2D(Curve geometry)
    {
        if(geometry is PolyCurve pCrv)
        {
            Geometry = pCrv.DuplicatePolyCurve();
            return;
        }

        pCrv = new PolyCurve();
        pCrv.Append(geometry);
        Geometry = pCrv.DuplicatePolyCurve();
    }

    public static PolyCurve2D? Create(Curve curve)
    {
        return new PolyCurve2D(curve);
    }

    public Brep ToBrep(double tolerence = 1e-5)
    {
        return Brep.CreatePlanarBreps(Geometry, tolerence).FirstOrDefault()!;
    }

    public static PolyCurve2D? Rectangle(Point2d min, Point2d max, double tolerence = 1e-5)
    {
        if (min.EpsilonEquals(max, tolerence))
            return null;

        var point1 = new Point3d(min.X, min.Y, 0);
        var point2 = new Point3d(max.X, min.Y, 0);
        var point3 = new Point3d(max.X, max.Y, 0);
        var point4 = new Point3d(min.X, max.Y, 0);

        var polyline = new PolylineCurve([point1, point2, point3, point4]);

        return new PolyCurve2D(polyline);
    }

    public double Area()
    {
        return AreaMassProperties.Compute(Geometry)!.Area;
    }

    public Point3d Centroid()
    {
        return AreaMassProperties.Compute(Geometry)!.Centroid;
    }

    public double Perimeter()
    {
        return Geometry.GetLength();
    }
}
