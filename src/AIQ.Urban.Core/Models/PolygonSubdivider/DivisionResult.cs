using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.Models.PolygonSubdivider;
public class DivisionResult
{
    public List<Profile2D> Fragmenets { get; }
    public List<Curve2D> Dividers { get; }
}
