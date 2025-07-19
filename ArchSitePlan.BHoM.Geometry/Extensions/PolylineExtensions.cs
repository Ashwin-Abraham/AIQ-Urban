using BH.Engine.Geometry;
using BH.Engine.Geometry.Triangulation;
using BH.oM.Geometry;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchSitePlan.BHoM.Geometry.Extensions;

public static class PolylineExtensions
{
    private sealed class FaceId(int a, int b, int c)
    {
        public readonly int a = a;
        public readonly int b = b;
        public readonly int c = c;
    }

    public static ICurve ClosedPolylineToPolygon(this Polyline polyline)
    {
        var points = polyline.IControlPoints();

        if (points.Count > 2 && polyline.IsClosed())
            return Create.Polygon(points.Take(points.Count -1).ToList());

        return polyline;
    }

    public static Mesh ToMesh(this Polyline polyline)
    {
        var triangles = polyline.DelaunayTriangulation();

        List<Point> vertices = new();
        List<FaceId> faceIds = new();

        foreach(var triangle in triangles)
        {
            var triangelVerts = triangle.Vertices();
            int a = vertices.FindIndex(x => x.AlmostEquals(triangelVerts[0]));
            if(a == -1)
            {
                vertices.Add(triangelVerts[0]);
                a = vertices.Count - 1;
            }

            int b = vertices.FindIndex(x => x.AlmostEquals(triangelVerts[1]));
            if (b == -1)
            {
                vertices.Add(triangelVerts[1]);
                b = vertices.Count - 1;
            }

            int c = vertices.FindIndex(x => x.AlmostEquals(triangelVerts[2]));
            if (b == -1)
            {
                vertices.Add(triangelVerts[2]);
                b = vertices.Count - 1;
            }

            faceIds.Add(new(a, b, c));
        }

        var faces = faceIds.Select(x => Create.Face(x.a, x.b, x.c));

        var mesh = Create.Mesh(vertices, faces);
        return mesh;
    }

    public static List<Line> Segments(this Polyline polyline)
    {
        List<Line> segments = new();
        
        for(int i =0; i< polyline.ControlPoints.Count - 1; i++)
            segments.Add(Create.Line(polyline.ControlPoints[i], polyline.ControlPoints[i+1]));

        return segments;
    }

    public static PolyCurve Fillet(this Polyline polyline, double radius)
    {
        var polyCurveSegments = new List<ICurve>();

        var segments = polyline.Segments();
        for(int i = 0; i< segments.Count -1; i++)
        {
            var filletted = segments[i].ArcBetweenLinesThatMeet(segments[i+1], radius);
            polyCurveSegments.AddRange(filletted.Curves);
        }

        return Create.PolyCurve(polyCurveSegments);
    }
}
