using ArchSitePlan.BHoM.Geometry.Comparers;
using ArchSitePlan.BHoM.Geometry.Extensions;
using BH.Engine.Geometry;
using BH.Engine.Geometry.Triangulation;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Models;

public static class MeshBuilder
{
    public static Mesh FromExtrudedSurface(PlanarSurface baseSurface, Vector extruder)
    {
        var outerCurve = baseSurface.ExternalBoundary.IToPolyline();
        var innerCurves = baseSurface.InternalBoundaries.Select(x => x.IToPolyline()).ToList();

        // Ensure Correct direction
        outerCurve = outerCurve.EnsureAntiClockwise() as Polyline;
        innerCurves = innerCurves.Select(x => (x.EnsureClockwise() as Polyline)!).ToList();

        var faceTriangles = outerCurve.DelaunayTriangulation(innerCurves);
        var movedTriangles = faceTriangles
            .Select(x => x.Translate(extruder))
            .Select(x => x.Flip())
            .ToList();
        faceTriangles.AddRange(movedTriangles);

        var sidePolylines = innerCurves.Append(outerCurve)
            .SelectMany(x => GetExtrudedFacesOutlines(x, extruder));

        var sideTriangles = sidePolylines.SelectMany(x =>
        {
            var normal = x.Normal();
            var triangles = x.DelaunayTriangulation();
            triangles = triangles.Select(x => x.EnsureClockwise(x.Normal()) as Polyline).ToList();
            return triangles;
        }).ToList();


        var comparer = new PointComparer(5);
        var vertices = faceTriangles
            .SelectMany(x => x.Vertices())
            .Distinct(comparer)
            .ToList();

        faceTriangles.AddRange(sideTriangles);

        var faceIds = faceTriangles
            .Select(face => FindTriangleVertexIndices(face, vertices))
            .Where(faceIds => faceIds.Count == 3);

        var faces = faceIds.Select(x => Create.Face(x[0], x[1], x[2])).ToList();
        
        var mesh = Create.Mesh(vertices, faces);
        var valid = mesh.IsValid();

        return mesh;
    }

    private static List<int> FindTriangleVertexIndices(Polyline polylineTriangles, List<Point> vertices)
    {
        List<int> ids = new();
        var polylinePoints = polylineTriangles.Vertices();
        if (polylinePoints.Count != 3) return new();

        foreach (var point in polylinePoints)
        {
            var index = vertices.FindIndex(x => point.AlmostEquals(x));
            if (index == -1) return new();
            ids.Add(index);
        }

        return ids;
    }

    private static List<Polyline> GetExtrudedFacesOutlines(Polyline baseCurve, Vector extruder)
    {
        var lines = baseCurve.SubParts();

        List<Polyline> outlines = new();

        foreach (var line in lines)
        {
            var movedLine = line.Translate(extruder);
            var outline = Create.Polygon(new List<Point>()
            {
                line.Start,
                line.End,
                movedLine.End,
                movedLine.Start
            });

            outlines.Add(outline);
        }

        return outlines;
    }
}
