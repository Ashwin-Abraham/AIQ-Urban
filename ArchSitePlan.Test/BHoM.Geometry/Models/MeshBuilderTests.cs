using ArchSitePlan.BHoM.Geometry.Models;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Test.BHoM.Geometry.Models;

public class MeshBuilderTests
{
    private static Polygon _rectangle = Create.Polygon(new List<Point>()
    {
        Point.Origin,
        Create.Point(1,0,0),
        Create.Point(1,1,0),
        Create.Point(0,1,0)
    });

    [Fact]
    public void CanCreateMesh_FromPlanarSurface()
    {
        var surface = Create.PlanarSurface(_rectangle.IFlip());
        var extruder = Create.Vector(0, 0, 1);

        var mesh = MeshBuilder.FromExtrudedSurface(surface, extruder);

        DEBUGMODEL.Reset();
        DEBUGMODEL.Debug(mesh.ToHypar(), material: DebugMaterials.Green);
        DEBUGMODEL.OpenAsGlb();
    }
}
