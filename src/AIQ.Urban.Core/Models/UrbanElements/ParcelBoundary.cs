using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.UrbanElements;

/// <summary>A user-drawn curve that splits or encloses parcels within transit-defined holes.</summary>
public class ParcelBoundary : IUrbanElement
{
    /// <summary>The boundary curve (open for splitting, closed for enclosure).</summary>
    public Curve2D Curve { get; }

    public Guid Id => Curve.Id;

    public ParcelBoundary(Curve2D curve)
    {
        Curve = curve;
    }

    public Point3d GetRepresentativeCenter()
    {
        return Curve.Geometry.PointAtNormalizedLength(0.5);
    }

    public List<Guid> GetGeometryIds()
    {
        return [Curve.Id];
    }
}
