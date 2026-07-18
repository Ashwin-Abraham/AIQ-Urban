using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.UrbanElements;

public class FloorPlate : IUrbanElement
{
    public Guid Id => Outline.Id;
    public Polygon2D Outline { get; }
    public Use Use { get; private set; }
    public Building? Parent { get; set; }

    public FloorPlate(Polygon2D outline, Use use)
    {
        Outline = outline;
        Use = use;
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

    internal double Area()
    {
        return Outline.Area();
    }

    public double Z()
    {
        return Outline.Geometry.PointAtStart.Z;
    }
}
