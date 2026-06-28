using AIQ.Urban.Core.GeometryOperations;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.Models.ShapeSubdividers;

/// <summary>
/// Divides a profile using a longitudinal axis and a delaunay quad algorithm. Tries to return only quads, but may have some triangles.
/// </summary>
public class AxisAndQuadSubdivision : IShapeSubdivider
{
    private Axis _axis;

    public AxisAndQuadSubdivision(Axis axis)
    {
        _axis = axis;
    }

    public DivisionResult Run(Profile2D profile)
    {
        // Divide the profile using delaunay triangulation
        var divided = SweepLineTriangulator.Triangulate(profile);

        // Apply triangle to quad conversion

        // 
        throw new NotImplementedException();
    }
}
