using ArchSitePlan.BHoM.Geometry.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Test.Geometry.Extensions;

public class PolylineExtensionTests
{
    [Fact]
    public void CanCreateMesh()
    {
        var polyline = Create.Polyline(new List<Point>()
        { Point.Origin,
        Create.Point(1,0),
        Create.Point(1,1),
        Create.Point(0,1)});

        var mesh = polyline.ToMesh();
    }
}
