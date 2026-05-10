using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.GeometryElements;

public class Profile2D : IGeometryElement<Curve>
{
    public Curve Geometry { get; }
    public Guid Id { get; set; } = Guid.NewGuid();

    public Profile2D(Curve geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        if (!TryValidatePlanar(geometry, out var error))
            throw new ArgumentException(error, nameof(geometry));

        Geometry = geometry;
    }

    private static bool TryValidatePlanar(Curve curve, out string? error)
    {
        if (!curve.TryGetPlane(out var plane))
        {
            error = "Curve must be planar.";
            return false;
        }

        var bbox = curve.GetBoundingBox(plane);
        if (Math.Abs(bbox.Max.Z - bbox.Min.Z) > 0.001)
        {
            error = "Curve has Z variation; Profile2D requires zero Z variation.";
            return false;
        }

        error = null;
        return true;
    }
}
