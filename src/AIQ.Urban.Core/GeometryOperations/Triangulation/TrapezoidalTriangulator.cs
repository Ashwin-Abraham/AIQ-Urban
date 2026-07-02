using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.GeometryOperations.Triangulation;

internal static class TrapezoidalTriangulator
{
    private const double AngleThreshold = 150.0;
    private const double AngleEpsilon = 0.1;

    internal static List<(Point3d A, Point3d B, Point3d C)> Triangulate(
        Profile2D profile, double tolerance = 1e-5)
    {
        var vertexRings = ExtractVertexRings(profile);
        if (vertexRings.Sum(r => r.Count) < 3)
            return [];

        var snappedRings = vertexRings
            .Select(ring => ring.Select(v => Snap(v, tolerance)).ToList())
            .ToList();

        var trapezoids = Trapezoidation(snappedRings);

        var inside = SelectInsideTrapezoids(trapezoids);
        if (inside.Count == 0)
            return [];

        var mountains = MakeMonotoneMountains(inside);

        var triangles = MakeTriangles(mountains);

        return triangles.Select(t => (
            new Point3d(t.A.X, t.A.Y, 0),
            new Point3d(t.B.X, t.B.Y, 0),
            new Point3d(t.C.X, t.C.Y, 0)
        )).ToList();
    }

    private static List<List<TriangulationVertex>> ExtractVertexRings(Profile2D profile)
    {
        var result = new List<List<TriangulationVertex>>();

        var outlinePts = profile.Outline.Points();
        var outlineRing = outlinePts.Select(p => new TriangulationVertex(p.X, p.Y)).ToList();
        RemoveDuplicateClosure(outlineRing);
        result.Add(outlineRing);

        foreach (var hole in profile.Holes)
        {
            var holePts = hole.Points();
            var holeRing = holePts.Select(p => new TriangulationVertex(p.X, p.Y)).ToList();
            RemoveDuplicateClosure(holeRing);
            result.Add(holeRing);
        }

        return result;
    }

    private static void RemoveDuplicateClosure(List<TriangulationVertex> ring)
    {
        if (ring.Count > 1 && ring[0] == ring[^1])
            ring.RemoveAt(ring.Count - 1);
    }

    private static TriangulationVertex Snap(TriangulationVertex v, double tolerance)
    {
        var scale = 1.0 / tolerance;
        return new TriangulationVertex(
            Math.Round(v.X * scale) / scale,
            Math.Round(v.Y * scale) / scale);
    }

    private static List<TriangulationTrapezoid> Trapezoidation(
        List<List<TriangulationVertex>> polygons)
    {
        var rng = new Random(42);
        var edges = new List<TriangulationEdge>();

        foreach (var polygon in polygons)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                var j = (i + 1) % polygon.Count;
                edges.Add(new TriangulationEdge(polygon[i], polygon[j]));
            }
        }

        for (int i = edges.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (edges[i], edges[j]) = (edges[j], edges[i]);
        }

        TriangulationTrapezoid.ClearEdgeRegistry();

        var searchTree = new TriangulationNode(new TriangulationTrapezoid());
        var alreadyInserted = new HashSet<TriangulationVertex>();

        foreach (var edge in edges)
        {
            var topJustInserted = alreadyInserted.Add(edge.TopVertex);
            if (topJustInserted)
                searchTree.InsertVertex(edge.TopVertex);

            var bottomJustInserted = alreadyInserted.Add(edge.BottomVertex);
            if (bottomJustInserted)
                searchTree.InsertVertex(edge.BottomVertex);

            searchTree.InsertEdge(edge, topJustInserted, bottomJustInserted);
        }

        return searchTree.GetAllTraps();
    }

    private static List<TriangulationTrapezoid> SelectInsideTrapezoids(
        List<TriangulationTrapezoid> allTrapezoids)
        => allTrapezoids.Where(t => t.IsInside).ToList();

    private static List<MonotoneMountain> MakeMonotoneMountains(
        List<TriangulationTrapezoid> insideTrapezoids)
    {
        var aboveVertexByBaseEdge = GroupVerticesByMountain(insideTrapezoids);

        var mountains = new List<MonotoneMountain>();

        foreach (var (baseEdge, aboveVertexMapping) in aboveVertexByBaseEdge)
        {
            MonotoneVertex? belowMonotoneVertex = null;
            var currentVertex = baseEdge.BottomVertex;
            var mountainCreated = false;

            while (true)
            {
                var currentMonotoneVertex = new MonotoneVertex(
                    vertex: currentVertex,
                    below: belowMonotoneVertex);

                if (belowMonotoneVertex is not null)
                    belowMonotoneVertex.Above = currentMonotoneVertex;

                if (!aboveVertexMapping.TryGetValue(currentVertex, out var aboveVertex))
                    break;

                currentVertex = aboveVertex;
                belowMonotoneVertex = currentMonotoneVertex;

                if (!mountainCreated)
                {
                    mountains.Add(new MonotoneMountain(currentMonotoneVertex, baseEdge));
                    mountainCreated = true;
                }
            }
        }

        return mountains;
    }

    private static Dictionary<TriangulationEdge, Dictionary<TriangulationVertex, TriangulationVertex>> GroupVerticesByMountain(
        List<TriangulationTrapezoid> trapezoids)
    {
        var result = new Dictionary<TriangulationEdge, Dictionary<TriangulationVertex, TriangulationVertex>>();

        foreach (var trap in trapezoids)
        {
            var mountainBases = new List<TriangulationEdge>();

            if (trap.LeftEdge is not null
                && (!trap.BottomVertex.HasValue || trap.BottomVertex.Value != trap.LeftEdge.Value.BottomVertex
                    || !trap.TopVertex.HasValue || trap.TopVertex.Value != trap.LeftEdge.Value.TopVertex))
            {
                mountainBases.Add(trap.LeftEdge.Value);
            }

            if (trap.RightEdge is not null
                && (!trap.BottomVertex.HasValue || trap.BottomVertex.Value != trap.RightEdge.Value.BottomVertex
                    || !trap.TopVertex.HasValue || trap.TopVertex.Value != trap.RightEdge.Value.TopVertex))
            {
                mountainBases.Add(trap.RightEdge.Value);
            }

            if (mountainBases.Count == 0)
                throw new InvalidOperationException("Inconsistent trapezoid.");

            foreach (var mountainBase in mountainBases)
            {
                if (!result.TryGetValue(mountainBase, out var mapping))
                {
                    mapping = [];
                    result[mountainBase] = mapping;
                }

                if (trap.BottomVertex is not null && trap.TopVertex is not null)
                    mapping[trap.BottomVertex.Value] = trap.TopVertex.Value;
            }
        }

        return result;
    }

    private static List<(TriangulationVertex A, TriangulationVertex B, TriangulationVertex C)> MakeTriangles(
        List<MonotoneMountain> mountains)
    {
        var triangles = new List<(TriangulationVertex A, TriangulationVertex B, TriangulationVertex C)>();

        foreach (var mountain in mountains)
            TriangulateMonotoneMountain(mountain, triangles);

        return triangles;
    }

    private static void TriangulateMonotoneMountain(
        MonotoneMountain mountain,
        List<(TriangulationVertex A, TriangulationVertex B, TriangulationVertex C)> triangles,
        double angleThreshold = AngleThreshold)
    {
        if (mountain.IsDegenerated)
            return;

        var firstNonBaseVertex = mountain.BottomVertex.Above!;

        var convexOrder = TriangulationVertex.CounterClockwise(
            mountain.Base.TopVertex,
            mountain.Base.BottomVertex,
            firstNonBaseVertex.Vertex);

        var currentVertex = firstNonBaseVertex;
        var angleCloserToThreshold = 180.0;

        while (!currentVertex.IsBaseVertex)
        {
            var below = currentVertex.Below!;
            var above = currentVertex.Above!;

            var currentVertexConvex = TriangulationVertex.CounterClockwise(
                below.Vertex, currentVertex.Vertex, above.Vertex) == convexOrder;

            if (!currentVertexConvex)
            {
                currentVertex = above;
                continue;
            }

            var angle = TriangulationVertex.Angle(
                below.Vertex, currentVertex.Vertex, above.Vertex);

            if (angle > angleThreshold)
            {
                currentVertex = above;

                if (angle < angleCloserToThreshold)
                    angleCloserToThreshold = angle;

                continue;
            }

            var (v1, v2, v3) = convexOrder
                ? (below.Vertex, currentVertex.Vertex, above.Vertex)
                : (below.Vertex, above.Vertex, currentVertex.Vertex);

            triangles.Add((v1, v2, v3));

            below.Above = above;
            above.Below = below;
            currentVertex = below.IsBaseVertex ? above : below;
        }

        if (!mountain.IsDegenerated)
            TriangulateMonotoneMountain(mountain, triangles, angleCloserToThreshold + AngleEpsilon);
    }
}
