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
