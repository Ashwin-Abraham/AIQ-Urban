using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.Models.UrbanElements;

public class Parcel:IUrbanElement
{
    public Polygon2D Outline { get; }
    public Use Use { get; }

    public Parcel(Polygon2D outline, Use use)
    {
        Outline = outline;
        Use = use;
    }

    public List<Guid> GetGeometryIds()
    {
        return [Outline.Id];
    }
}
