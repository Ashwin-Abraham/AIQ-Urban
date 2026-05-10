using Rhino.Geometry;
using AIQ.Urban.Core.Extensions;

namespace AIQ.Urban.Core.Models.GeometryElements;

public class Profile2D : IGeometryElement<Curve>
{
    public Curve Geometry { get; }
    public Guid Id { get; set; } = Guid.NewGuid();

    public Profile2D(Curve geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        if (!geometry.IsPlanarInXY())
            throw new ArgumentException("Profile2D requires a curve in an XY plane.", nameof(geometry));

        if (!geometry.IsClosed)
            throw new ArgumentException("Profile2D requires a closed curve.", nameof(geometry));

        Geometry = geometry;
    }
}
