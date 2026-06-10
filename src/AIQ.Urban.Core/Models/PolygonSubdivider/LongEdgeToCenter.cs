using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.PolygonSubdivider;
public class LongEdgeToCenter(int _dividerCount = 2) : IShapeSubdivider
{
    public DivisionResult Run(Profile2D profile)
    {
        var centroid = profile.Centroid();
        var segments = profile.Outline.Segments();

    }
}
