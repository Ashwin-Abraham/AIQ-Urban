using AIQ.Urban.Core.GeometryOperations;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Extensions;

public static class MeshExtensions
{
    public static Mesh ToQuadMesh(this Mesh mesh)
    {
        return TriangleToQuadConverter.Convert(mesh);
    }
}
