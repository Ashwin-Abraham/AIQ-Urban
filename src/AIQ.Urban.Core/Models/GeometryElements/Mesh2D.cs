using AIQ.Urban.Core.Extensions;
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

    public static Mesh2D Create(Profile2D profile)
    {
        var points = profile.Holes
            .SelectMany(x => x.Points())
            .Concat(profile.Outline.Points())
            .ToList();

        var edges = profile.Holes.Select(x => x.Geometry)
            .Append(profile.Outline.Geometry)
            .Select(x =>
                {
                    var ctrlPts = x.ToNurbsCurve().Points;
                    return ctrlPts.Select(controlPoint => new Point3d(controlPoint.X, controlPoint.Y, controlPoint.Z))
                })
            .ToList();

        Plane.FitPlaneToPoints(points, out var plane);
        var mesh = Mesh.CreateFromTessellation(points, edges, plane, false);

        return new Mesh2D(mesh);
    }
    
}
