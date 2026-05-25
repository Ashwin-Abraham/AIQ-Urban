using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.GeometryElements;

public class Curve2D : IGeometryElement<Curve>
{
    public Curve Geometry { get; }
    public Guid Id { get; set; }

    public Curve2D(Curve geometry, Guid id)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        if (!geometry.IsPlanar())
            throw new ArgumentException("Curve is not planar in an XY plane", nameof(geometry));

        Geometry = geometry;
        Id = id;
    }

    public static Curve2D Create(List<(double X, double Y)> coordinates)
    {
        if (coordinates.Count < 2)
            throw new ArgumentException("At least 2 coordinate pairs are required.", nameof(coordinates));

        var points = coordinates.Select(coord => new Point3d(coord.X, coord.Y, 0)).ToArray();
        var curve = new PolylineCurve(points);

        return new Curve2D(curve, Guid.NewGuid());
    }
}
