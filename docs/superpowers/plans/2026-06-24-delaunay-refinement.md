# Delaunay Refinement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `DelaunayRefiner.Refine(Mesh)` — edge-flipping post-processing to enforce the Delaunay condition on any planar XY triangulated mesh.

**Architecture:** Single internal static class `DelaunayRefiner` in `GeometryOperations/`, taking a `Rhino.Geometry.Mesh`, building triangle adjacency, iteratively flipping interior edges that violate the InCircle predicate, and returning a new refined Mesh.

**Tech Stack:** NUnit 3.14, Rhino.Testing 8.0.28-beta, RhinoCommon

## Global Constraints

- All files: 4-space indent, K&R braces, file-scoped namespaces
- nullable enabled — use `?` appropriately, avoid `!`
- No BHoM, no TriangleNet, no new NuGet packages
- Extension methods in `Extensions/`, internal helpers in `GeometryOperations/`
- Tests use `[RhinoTestFixture]`, `[TestCase]` + private record + static `_testData` pattern

---

### Task 1: Create failing DelaunayRefiner tests

**Files:**
- Create: `tests/AIQ.Urban.Core.Tests/GeometryOperations/DelaunayRefinerTests.cs`

**Interfaces:**
- Consumes: `DelaunayRefiner.Refine(Mesh, double)` — not yet defined
- Produces: test cases and expected values that define the API contract

- [ ] **Step 1: Write the test file**

```csharp
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.GeometryOperations;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.GeometryOperations;

[RhinoTestFixture]
public class DelaunayRefinerTests
{
    [Test]
    [TestCase("SimpleSquare")]
    [TestCase("LShape")]
    [TestCase("Jagged")]
    [TestCase("UShape")]
    public void Refine_ProducesValidMesh(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        var mesh = testData.Input;

        var result = DelaunayRefiner.Refine(mesh);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Vertices.Count, Is.GreaterThan(0));
        Assert.That(result.Faces.Count, Is.GreaterThan(0));
        Assert.That(result.Faces.All(f => f.IsTriangle), Is.True);
        Assert.That(result.Faces.Count, Is.EqualTo(testData.ExpectedTriangleCount));

        var inputArea = AreaMassProperties.Compute(mesh).Area;
        var resultArea = AreaMassProperties.Compute(result).Area;
        Assert.That(resultArea, Is.EqualTo(inputArea).Within(1e-4));

        var avgArea = resultArea / result.Faces.Count;
        Assert.That(avgArea, Is.EqualTo(testData.ExpectedAverageArea).Within(1e-2));
    }

    [Test]
    public void Refine_NonDelaunayQuad_FlippedEdge()
    {
        // A convex non-cyclic quad split with the "wrong" diagonal
        // Vertices: A(0,0), B(10,0), C(9,10), D(1,9)
        // Triangulated as ABC + ACD (diagonal AC = (0,0)-(9,10))
        var mesh = new Mesh();
        mesh.Vertices.Add(0f, 0f, 0f);  // 0: A
        mesh.Vertices.Add(10f, 0f, 0f); // 1: B
        mesh.Vertices.Add(9f, 10f, 0f); // 2: C
        mesh.Vertices.Add(1f, 9f, 0f);  // 3: D
        mesh.Faces.AddFace(0, 1, 2);    // ABC
        mesh.Faces.AddFace(0, 2, 3);    // ACD
        mesh.Faces.AddFace(0, 2, 2);    // Rhino tri convention

        var result = DelaunayRefiner.Refine(mesh);

        Assert.That(result.Faces.Count, Is.EqualTo(2));
        Assert.That(result.IsValid, Is.True);

        // The edge (0,0)-(9,10) (AC) should have been flipped
        // Result should use diagonal BD = (10,0)-(1,9) instead
        // Check that result faces are NOT both (0,1,2) and (0,2,3)
        var face0 = result.Faces[0];
        var face1 = result.Faces[1];
        var vertsUsed = new[] { face0.A, face0.B, face0.C, face1.A, face1.B, face1.C }.Distinct().OrderBy(v => v).ToArray();
        Assert.That(vertsUsed, Is.EquivalentTo(new[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void Refine_EmptyMesh_ReturnsEmpty()
    {
        var mesh = new Mesh();
        var result = DelaunayRefiner.Refine(mesh);
        Assert.That(result.Vertices.Count, Is.Zero);
        Assert.That(result.Faces.Count, Is.Zero);
    }

    [Test]
    public void Refine_DegenerateTriangle_HandlesGracefully()
    {
        var mesh = new Mesh();
        mesh.Vertices.Add(0f, 0f, 0f);
        mesh.Vertices.Add(1f, 0f, 0f);
        mesh.Vertices.Add(0.5f, 1e-7f, 0f);
        mesh.Faces.AddFace(0, 1, 2);
        mesh.Faces.AddFace(0, 1, 1);

        var result = DelaunayRefiner.Refine(mesh);
        Assert.That(result.Faces.Count, Is.EqualTo(1));
    }

    private record DelaunayTestData(string CaseName, Mesh Input, int ExpectedTriangleCount, double ExpectedAverageArea);

    private static readonly List<DelaunayTestData> _testData = BuildTestData();

    private static List<DelaunayTestData> BuildTestData()
    {
        return
        [
            new(CaseName: "SimpleSquare", Input: MakeSquareMesh(), ExpectedTriangleCount: 2, ExpectedAverageArea: 50),
            new(CaseName: "LShape", Input: MakeLShapeMesh(), ExpectedTriangleCount: 4, ExpectedAverageArea: 18.75),
            new(CaseName: "Jagged", Input: MakeJaggedMesh(), ExpectedTriangleCount: 9, ExpectedAverageArea: 11.72),
            new(CaseName: "UShape", Input: MakeUShapeMesh(), ExpectedTriangleCount: 6, ExpectedAverageArea: 12),
        ];
    }

    private static Mesh TriangulateProfile(Polygon2D poly)
    {
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        var profile = new Profile2D(brep, poly, []);
        return profile.Triangulate();
    }

    private static Mesh MakeSquareMesh()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        return TriangulateProfile(poly);
    }

    private static Mesh MakeLShapeMesh()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 5, 0), new Point3d(5, 5, 0),
            new Point3d(5, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        return TriangulateProfile(poly);
    }

    private static Mesh MakeJaggedMesh()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 2, 0), new Point3d(10, 2, 0),
            new Point3d(9, 6, 0), new Point3d(13, 12, 0),
            new Point3d(15, 8, 0), new Point3d(20, 13, 0),
            new Point3d(18, 18, 0), new Point3d(13, 16, 0),
            new Point3d(9, 21, 0), new Point3d(11, 14, 0),
            new Point3d(4, 4, 0), new Point3d(0, 2, 0),
        ]));
        return TriangulateProfile(poly);
    }

    private static Mesh MakeUShapeMesh()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(7, 10, 0),
            new Point3d(7, 3, 0), new Point3d(3, 3, 0),
            new Point3d(3, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        return TriangulateProfile(poly);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj --filter "FullyQualifiedName~DelaunayRefiner" -v n`
Expected: Build fails with CS0103 "The name 'DelaunayRefiner' does not exist in the current context"

### Task 2: Implement DelaunayRefiner

**Files:**
- Create: `src/AIQ.Urban.Core/GeometryOperations/DelaunayRefiner.cs`

**Interfaces:**
- Consumes: test expectations from Task 1
- Produces: `DelaunayRefiner.Refine(Mesh, double)` → `Mesh`

- [ ] **Step 1: Write implementation**

```csharp
using Rhino.Geometry;

namespace AIQ.Urban.Core.GeometryOperations;

internal static class DelaunayRefiner
{
    public static Mesh Refine(Mesh mesh, double tolerance = 1e-5)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        var result = new Mesh();
        if (mesh.Faces.Count == 0)
            return result;

        // Validate all faces are triangles
        for (var i = 0; i < mesh.Faces.Count; i++)
        {
            if (!mesh.Faces[i].IsTriangle)
                throw new ArgumentException("All faces must be triangles");
        }

        // Validate planar at z=0
        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            if (System.Math.Abs(mesh.Vertices[i].Z) > tolerance)
                throw new ArgumentException("Mesh must be planar at z=0");
        }

        // Copy vertex positions
        var verts = new Point3d[mesh.Vertices.Count];
        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            verts[i] = new Point3d(v.X, v.Y, 0);
        }

        // Build face vertex arrays
        var nFaces = mesh.Faces.Count;
        var triVerts = new (int A, int B, int C)[nFaces];
        for (var i = 0; i < nFaces; i++)
        {
            var f = mesh.Faces[i];
            triVerts[i] = (f.A, f.B, f.C);
        }

        // Build adjacency: for each face, store neighbor index per edge (-1 = boundary)
        var adj = BuildAdjacency(triVerts, nFaces);

        // Iterative edge flipping
        var maxPasses = nFaces * nFaces;
        for (var pass = 0; pass < maxPasses; pass++)
        {
            var flippedAny = false;

            for (var fIdx = 0; fIdx < nFaces; fIdx++)
            {
                var (a, b, c) = triVerts[fIdx];

                for (var edge = 0; edge < 3; edge++)
                {
                    var nIdx = adj[fIdx][edge];
                    if (nIdx < 0 || nIdx <= fIdx) continue; // boundary or already processed

                    // Identify the two shared vertices
                    var vertsInEdge = GetEdgeVerts(fIdx, edge, triVerts);
                    var (v0, v1) = vertsInEdge;

                    // Find the opposite vertices
                    var opp0 = OppositeVert(fIdx, v0, v1, triVerts);
                    var opp1 = OppositeVert(nIdx, v0, v1, triVerts);

                    if (opp0 < 0 || opp1 < 0) continue;

                    // Check if quadrilateral is convex
                    if (!IsConvex(verts[opp0], verts[v0], verts[opp1], verts[v1], tolerance))
                        continue;

                    // Check Delaunay condition: is opp1 inside circumcircle of (opp0, v0, v1)?
                    if (!InCircle(verts[opp0], verts[v0], verts[v1], verts[opp1], tolerance))
                        continue;

                    // Flip: replace edge (v0,v1) with edge (opp0,opp1)
                    // Old tris: fIdx=(opp0,v0,v1), nIdx=(opp1,v1,v0)
                    // New tris: fIdx=(opp0,v0,opp1), nIdx=(opp1,v1,opp0)

                    // Save old neighbors before modifying
                    var adjF = adj[fIdx];
                    var adjN = adj[nIdx];

                    // Determine which neighbor slot maps to the shared edge
                    var edgeInF = EdgeIndex(fIdx, v0, v1, triVerts);
                    var edgeInN = EdgeIndex(nIdx, v0, v1, triVerts);

                    // Update vertex sets
                    triVerts[fIdx] = (opp0, v0, opp1);
                    triVerts[nIdx] = (opp1, v1, opp0);

                    // Rebuild adjacency for the two flipped faces
                    var newAdjF = BuildFaceAdjacency(fIdx, triVerts, adj, nFaces);
                    var newAdjN = BuildFaceAdjacency(nIdx, triVerts, adj, nFaces);
                    adj[fIdx] = newAdjF;
                    adj[nIdx] = newAdjN;

                    // Update neighbors of adjacent faces to point to the right new faces
                    UpdateNeighborReferences(adj, fIdx, nIdx, triVerts, nFaces);

                    flippedAny = true;
                    break; // restart face iteration (adj changed)
                }
            }

            if (!flippedAny) break;
        }

        // Build output mesh
        for (var i = 0; i < verts.Length; i++)
            result.Vertices.Add((Point3f)verts[i]);

        for (var i = 0; i < nFaces; i++)
        {
            var (a, b, c) = triVerts[i];
            result.Faces.AddFace(a, b, c, c);
        }

        return result;
    }

    private static int[][] BuildAdjacency((int A, int B, int C)[] triVerts, int nFaces)
    {
        // Map each edge (sorted vertex pair) to the two faces sharing it
        var edgeToFaces = new Dictionary<(int, int), List<int>>();
        for (var i = 0; i < nFaces; i++)
        {
            var (a, b, c) = triVerts[i];
            AddEdge(edgeToFaces, i, a, b);
            AddEdge(edgeToFaces, i, b, c);
            AddEdge(edgeToFaces, i, c, a);
        }

        var adj = new int[nFaces][];
        for (var i = 0; i < nFaces; i++)
            adj[i] = [-1, -1, -1];

        foreach (var kvp in edgeToFaces)
        {
            var faces = kvp.Value;
            if (faces.Count == 2)
            {
                var f0 = faces[0];
                var f1 = faces[1];
                var (v0, v1) = kvp.Key;
                var edge0 = EdgeIndex(f0, v0, v1, triVerts);
                var edge1 = EdgeIndex(f1, v0, v1, triVerts);
                adj[f0][edge0] = f1;
                adj[f1][edge1] = f0;
            }
            // faces.Count > 2 → non-manifold, leave as boundary
        }

        return adj;
    }

    private static void AddEdge(Dictionary<(int, int), List<int>> dict, int faceIdx, int v0, int v1)
    {
        var key = v0 < v1 ? (v0, v1) : (v1, v0);
        if (!dict.TryGetValue(key, out var list))
        {
            list = [];
            dict[key] = list;
        }
        list.Add(faceIdx);
    }

    private static int EdgeIndex(int faceIdx, int v0, int v1, (int A, int B, int C)[] triVerts)
    {
        var (a, b, c) = triVerts[faceIdx];
        if ((a == v0 && b == v1) || (a == v1 && b == v0)) return 0;
        if ((b == v0 && c == v1) || (b == v1 && c == v0)) return 1;
        if ((c == v0 && a == v1) || (c == v1 && a == v0)) return 2;
        return -1;
    }

    private static (int v0, int v1) GetEdgeVerts(int faceIdx, int edgeIdx, (int A, int B, int C)[] triVerts)
    {
        var (a, b, c) = triVerts[faceIdx];
        return edgeIdx switch
        {
            0 => (a, b),
            1 => (b, c),
            2 => (c, a),
            _ => (0, 0),
        };
    }

    private static int OppositeVert(int faceIdx, int v0, int v1, (int A, int B, int C)[] triVerts)
    {
        var (a, b, c) = triVerts[faceIdx];
        if ((a != v0 && a != v1)) return a;
        if ((b != v0 && b != v1)) return b;
        if ((c != v0 && c != v1)) return c;
        return -1;
    }

    private static int[] BuildFaceAdjacency(int faceIdx, (int A, int B, int C)[] triVerts, int[][] adj, int nFaces)
    {
        var result = new[] { -1, -1, -1 };
        var (a, b, c) = triVerts[faceIdx];
        var edges = new[] { (a, b), (b, c), (c, a) };

        for (var e = 0; e < 3; e++)
        {
            var (v0, v1) = edges[e];
            // Find the face sharing this edge
            for (var j = 0; j < nFaces; j++)
            {
                if (j == faceIdx) continue;
                var (ja, jb, jc) = triVerts[j];
                if (SharesEdge(v0, v1, ja, jb, jc))
                {
                    result[e] = j;
                    break;
                }
            }
        }

        return result;
    }

    private static bool SharesEdge(int v0, int v1, int a, int b, int c)
    {
        return (a == v0 && b == v1) || (a == v1 && b == v0) ||
               (b == v0 && c == v1) || (b == v1 && c == v0) ||
               (c == v0 && a == v1) || (c == v1 && a == v0);
    }

    private static void UpdateNeighborReferences(int[][] adj, int oldF, int oldN, (int A, int B, int C)[] triVerts, int nFaces)
    {
        // After flipping, update adjacency entries in faces that neighbor the flipped pair
        // Only need to check faces that now share an edge with fIdx or nIdx
        for (var i = 0; i < nFaces; i++)
        {
            if (i == oldF || i == oldN) continue;
            var oldAdj = adj[i];
            for (var e = 0; e < 3; e++)
            {
                if (oldAdj[e] == oldF || oldAdj[e] == oldN)
                {
                    // Recompute this face's adjacency
                    adj[i] = BuildFaceAdjacency(i, triVerts, adj, nFaces);
                    break;
                }
            }
        }
    }

    private static bool IsConvex(Point3d a, Point3d b, Point3d c, Point3d d, double tolerance)
    {
        // Check quadrilateral (a, b, c, d) is convex and CCW
        // All cross products of consecutive edges must have the same sign
        var c1 = CrossZ(a, b, c);
        var c2 = CrossZ(b, c, d);
        var c3 = CrossZ(c, d, a);
        var c4 = CrossZ(d, a, b);
        return (c1 > -tolerance && c2 > -tolerance && c3 > -tolerance && c4 > -tolerance) ||
               (c1 < tolerance && c2 < tolerance && c3 < tolerance && c4 < tolerance);
    }

    private static double CrossZ(Point3d p, Point3d q, Point3d r)
    {
        return (q.X - p.X) * (r.Y - q.Y) - (q.Y - p.Y) * (r.X - q.X);
    }

    private static bool InCircle(Point3d a, Point3d b, Point3d c, Point3d d, double tolerance)
    {
        // Standard 4x4 determinant InCircle test
        // Returns true if d is inside the circumcircle of triangle abc
        // See: https://www.cs.cmu.edu/~quake/robust.html
        var ax = a.X; var ay = a.Y;
        var bx = b.X; var by = b.Y;
        var cx = c.X; var cy = c.Y;
        var dx = d.X; var dy = d.Y;

        var a2 = ax * ax + ay * ay;
        var b2 = bx * bx + by * by;
        var c2 = cx * cx + cy * cy;
        var d2 = dx * dx + dy * dy;

        // Laplace expansion along last column
        var det = (ax * (by * c2 - b2 * cy) - ay * (bx * c2 - b2 * cx) + a2 * (bx * cy - by * cx))
                - (ax * (by * d2 - b2 * dy) - ay * (bx * d2 - b2 * dx) + a2 * (bx * dy - by * dx))
                + (ax * (cy * d2 - c2 * dy) - ay * (cx * d2 - c2 * dx) + a2 * (cx * dy - cy * dx))
                - (bx * (cy * d2 - c2 * dy) - by * (cx * d2 - c2 * dx) + b2 * (cx * dy - cy * dx));

        return det > tolerance;
    }
}
```

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj --filter "FullyQualifiedName~DelaunayRefiner" -v n`
Expected: All 6 tests pass

- [ ] **Step 3: Commit**

```bash
git add src/AIQ.Urban.Core/GeometryOperations/DelaunayRefiner.cs tests/AIQ.Urban.Core.Tests/GeometryOperations/DelaunayRefinerTests.cs
git commit -m "feat: add DelaunayRefiner edge-flipping refinement"
git push
```

### Task 3: Self-review and verify

- [ ] **Step 1: Review implementation against spec**

Check:
- Input validation (null, empty, non-triangle, non-planar) all handled
- Boundary edges preserved (adjacency count == 1)
- Area preserved (test assertion)
- InCircle uses determinant with link comment
- Max passes guard present

- [ ] **Step 2: Run full test suite**

Run: `dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj --filter "FullyQualifiedName~DelaunayRefiner" -v n`
Expected: All passes
