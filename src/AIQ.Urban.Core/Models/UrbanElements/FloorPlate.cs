using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.UrbanElements;

public class FloorPlate : IUrbanElement
{
    public Polygon2D Outline { get; }
    public Use Use { get; }

    public FloorPlate(Polygon2D outline, Use use)
    {
        Outline = outline;
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
