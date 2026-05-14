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
}
