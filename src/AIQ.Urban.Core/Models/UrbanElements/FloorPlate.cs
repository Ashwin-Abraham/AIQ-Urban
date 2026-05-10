using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.Models.UrbanElements;

public class FloorPlate:IUrbanElement
{
    public Polygon2D Outline { get; }
    public Use Use { get; }

    public FloorPlate(Polygon2D outline, Use use)
    {
        Outline = outline;
        Use = use;
    }

    public List<Guid> GetGeometryIds()
    {
        return [Outline.Id];
    }
}
