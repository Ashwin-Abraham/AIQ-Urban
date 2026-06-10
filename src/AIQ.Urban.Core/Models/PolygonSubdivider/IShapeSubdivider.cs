using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.Models.PolygonSubdivider;
public interface IShapeSubdivider
{
    public DivisionResult Run(Profile2D polygon);
}
