using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanGraph;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.UrbanElements;

public class Parcel : IUrbanElement, IGraphableAsNode
{
    public Polycurve2D Outline { get; }
    public Point3d NodePoint { get; }

    public Use Use { get; private set; }

    public Guid Id => Outline.Id;

    public Parcel(Polycurve2D outline, Use use)
    {
        Outline = outline;
        Use = use;
        NodePoint = GetRepresentativeCenter();
    }

    public void SetUse(Use use)
    {
        Use = use;
    }

    public Point3d GetRepresentativeCenter()
    {
        return AreaMassProperties.Compute(Outline.Geometry)!.Centroid;
    }

    public List<Guid> GetGeometryIds()
    {
        return [Outline.Id];
    }
}
