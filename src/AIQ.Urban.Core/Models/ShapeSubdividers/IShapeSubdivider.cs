using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.Models.ShapeSubdividers;
public interface IShapeSubdivider
{
    public DivisionResult Run(Profile2D profile);
}
