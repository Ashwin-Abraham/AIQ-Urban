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

    public static Curve2D Create(double[][] coords)
    {
        ArgumentNullException.ThrowIfNull(coords);

        if (coords.Length < 2)
            throw new ArgumentException("At least 2 coordinate pairs are required.", nameof(coords));

        var points = coords.Select(c => new Point3d(c[0], c[1], 0)).ToArray();
        var curve = new PolylineCurve(points);

        return new Curve2D(curve, Guid.NewGuid());
    }
}
