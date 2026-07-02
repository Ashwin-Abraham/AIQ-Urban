using Rhino.Geometry;

namespace AIQ.Urban.Core.Models.GeometryElements;
public class Mesh2D : IGeometryElement<Mesh>
{
    public Mesh Geometry { get; }
    public Guid Id { get; set; }

    public Mesh2D(Mesh geometry, Guid? id = null)
    {
        if (!Validate2D(geometry))
            throw new ArgumentException("Mesh is not flat");

        Geometry = geometry;
        Id = id?? Guid.NewGuid();
    }

    private static bool Validate2D(Mesh geometry)
    {
        var points = geometry.Vertices.Select(v => new Point3d(v.X, v.Y, v.Z)).ToList();

        if (points.Count == 0)
            return false;

        return Plane.FitPlaneToPoints(points, out _) == PlaneFitResult.Success;
    }
    
}
