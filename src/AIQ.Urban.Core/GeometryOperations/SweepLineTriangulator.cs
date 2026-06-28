using AIQ.Primitives.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.GeometryOperations;

internal static class SweepLineTriangulator
{
    public static Mesh Triangulate(Profile2D profile, double tolerance = 1e-5)
    {
        var result = new Mesh();

        // Merge holes into outer contour via bridging, producing a simple polygon
        var polygon = MergeContours(
            outer: profile.Outline.Points(), 
            holes: profile.Holes.Select(x => x.Points()).ToList(), 
            tolerance);

        if (polygon.Count < 3 || Math.Abs(SignedArea(polygon)) < tolerance)
            return result;

        // Ensure CCW winding for the merged polygon
        if (SignedArea(polygon) < 0)
            polygon.Reverse();

        // Triangulate via ear clipping
        var triangles = EarClip(polygon, tolerance);

        // Build Rhino Mesh from triangle vertex indices
        BuildMesh(result, polygon, triangles);
        return result;
    }

    private static List<Point3d> MergeContours(
        IReadOnlyList<Point3d> outer,
        IReadOnlyList<IReadOnlyList<Point3d>> holes,
        double tolerance)
    {
        var polygon = new List<Point3d>(outer);

        foreach (var hole in holes)
        {
            if (hole.Count < 3) continue;

            // Ensure hole is CW (negative area)
            var holePoints = new List<Point3d>(hole);
            if (SignedArea(holePoints) > 0)
                holePoints.Reverse();

            // Find the rightmost vertex of the hole (highest X, then highest Y)
            var rightmostIdx = FindRightmostIndex(holePoints, tolerance);
            var rightmost = holePoints[rightmostIdx];

            // Find the closest visible edge on the outer polygon to the right
            var (bridgeToPolyIdx, bridgeToHoleIdx) = FindBridge(polygon, holePoints, rightmostIdx, tolerance);

            // Merge: splice hole into polygon at bridge location
            var merged = new List<Point3d>(polygon.Count + holePoints.Count + 2);
            for (var i = 0; i <= bridgeToPolyIdx; i++)
                merged.Add(polygon[i]);

            // Insert hole vertices starting from bridgeToHoleIdx
            for (var i = 0; i < holePoints.Count; i++)
            {
                var holeIdx = (bridgeToHoleIdx + i) % holePoints.Count;
                merged.Add(holePoints[holeIdx]);
            }

            // Repeat the bridge vertex (doubled edge)
            merged.Add(holePoints[bridgeToHoleIdx]);

            // Continue outer polygon from bridgeToPolyIdx
            for (var i = bridgeToPolyIdx; i < polygon.Count; i++)
                merged.Add(polygon[i]);

            polygon = merged;
        }

        return polygon;
    }

    private static int FindRightmostIndex(List<Point3d> points, double tolerance)
    {
        var best = 0;
        for (var i = 1; i < points.Count; i++)
        {
            if (points[i].X > points[best].X ||
                (points[i].X.AlmostEquals(points[best].X, tolerance) && points[i].Y > points[best].Y))
            {
                best = i;
            }
        }
        return best;
    }

    private static (int PolyIdx, int HoleIdx) FindBridge(
        List<Point3d> polygon,
        List<Point3d> holePoints,
        int holeRightmostIdx,
        double tolerance)
    {
        var hp = holePoints[holeRightmostIdx];

        // Find the edge in polygon that intersects a ray to the right from hp
        var bestPolyIdx = -1;
        var bestHoleIdx = -1;
        var bestDist = double.MaxValue;

        for (var i = 0; i < polygon.Count; i++)
        {
            var j = (i + 1) % polygon.Count;
            var a = polygon[i];
            var b = polygon[j];

            // Edge must be to the right of hp (at least partially)
            if (a.X <= hp.X && b.X <= hp.X) continue;

            // Check if the horizontal ray to the right from hp intersects edge (a,b)
            if ((a.Y > hp.Y) == (b.Y > hp.Y)) continue; // no intersection

            var dy = b.Y - a.Y;
            if (Math.Abs(dy) < tolerance) continue; // horizontal edge, ray runs parallel

            var xIntersect = a.X + (hp.Y - a.Y) * (b.X - a.X) / dy;
            if (xIntersect <= hp.X) continue;

            // Found a closer intersection
            var dist = xIntersect - hp.X;
            if (dist < bestDist)
            {
                // Check if the intersection point is on the edge, not at a vertex
                bestDist = dist;
                bestPolyIdx = j; // use vertex j as the target (next vertex)

                // Find the corresponding hole vertex index for the bridge
                // Use the rightmost vertex of the hole
                bestHoleIdx = holeRightmostIdx;
            }
        }

        if (bestPolyIdx < 0)
        {
            // Fallback: connect to the closest polygon vertex
            var closest = 0;
            var closestDist = hp.DistanceTo(polygon[0]);
            for (var i = 1; i < polygon.Count; i++)
            {
                var d = hp.DistanceTo(polygon[i]);
                if (d < closestDist)
                {
                    closestDist = d;
                    closest = i;
                }
            }
            return (closest, holeRightmostIdx);
        }

        return (bestPolyIdx, bestHoleIdx);
    }

    private static List<(int A, int B, int C)> EarClip(List<Point3d> polygon, double tolerance)
    {
        var n = polygon.Count;
        if (n < 3) return [];

        var triangles = new List<(int A, int B, int C)>();
        var remaining = new List<int>(n);
        for (var i = 0; i < n; i++)
            remaining.Add(i);

        var prev = new List<int>(n);
        var next = new List<int>(n);
        for (var i = 0; i < n; i++)
        {
            prev.Add((i - 1 + n) % n);
            next.Add((i + 1) % n);
        }

        var iIdx = 0;
        var maxIterations = n * n; // safety limit
        while (remaining.Count > 3 && maxIterations-- > 0)
        {
            var foundEar = false;
            var startIdx = iIdx;

            for (var iter = 0; iter < remaining.Count; iter++)
            {
                var vi = remaining[iIdx];
                var vj = next[vi];
                var vk = next[vj];

                var pi = polygon[vi];
                var pj = polygon[vj];
                var pk = polygon[vk];

                // Check if vj is convex (interior angle < 180 for CCW)
                var v1 = pi - pj;
                var v2 = pk - pj;
                var cross = v2.X * v1.Y - v2.Y * v1.X;
                if (cross <= 0) // not convex (or degenerate)
                {
                    iIdx = (iIdx + 1) % remaining.Count;
                    continue;
                }

                // Check if triangle (vi, vj, vk) contains any other vertices
                var isEar = true;
                for (var t = 0; t < remaining.Count; t++)
                {
                    var testIdx = remaining[t];
                    if (testIdx == vi || testIdx == vj || testIdx == vk)
                        continue;

                    if (PointInTriangle(polygon[testIdx], pi, pj, pk, tolerance))
                    {
                        isEar = false;
                        break;
                    }
                }

                if (isEar)
                {
                    triangles.Add((vi, vj, vk));

                    // Remove vj from the linked list and remaining list
                    prev[next[vj]] = prev[vj];
                    next[prev[vj]] = next[vj];
                    var vjPos = remaining.IndexOf(vj);
                    remaining.RemoveAt(vjPos);
                    if (vjPos < iIdx)
                        iIdx--;

                    foundEar = true;
                    break;
                }

                iIdx = (iIdx + 1) % remaining.Count;
            }

            if (!foundEar)
                break;
        }

        // Add the final triangle
        if (remaining.Count == 3)
        {
            triangles.Add((remaining[0], remaining[1], remaining[2]));
        }

        return triangles;
    }

    private static bool PointInTriangle(Point3d pt, Point3d a, Point3d b, Point3d c, double tolerance)
    {
        var d1 = Sign(pt, a, b);
        var d2 = Sign(pt, b, c);
        var d3 = Sign(pt, c, a);
        return (d1 > tolerance && d2 > tolerance && d3 > tolerance) ||
               (d1 < -tolerance && d2 < -tolerance && d3 < -tolerance);
    }

    private static double Sign(Point3d p1, Point3d p2, Point3d p3)
    {
        return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
    }

    private static double SignedArea(IReadOnlyList<Point3d> contour)
    {
        var area = 0.0;
        var n = contour.Count;
        for (var i = 0; i < n; i++)
        {
            var a = contour[i];
            var b = contour[(i + 1) % n];
            area += a.X * b.Y - b.X * a.Y;
        }
        return area / 2.0;
    }

    private static void BuildMesh(
        Mesh mesh,
        List<Point3d> polygon,
        List<(int A, int B, int C)> triangles)
    {
        if (triangles.Count == 0) return;

        // Add all vertices to mesh
        var vertMap = new int[polygon.Count];
        for (var i = 0; i < polygon.Count; i++)
        {
            vertMap[i] = mesh.Vertices.Add((Point3f)polygon[i]);
        }

        foreach (var tri in triangles)
        {
            mesh.Faces.AddFace(vertMap[tri.A], vertMap[tri.B], vertMap[tri.C], vertMap[tri.C]);
        }
    }
}
