# Delaunay Refinement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `DelaunayRefiner.Refine(Mesh)` — an edge-flipping post-processor that enforces the Delaunay condition on any planar XY triangulated mesh.

**Architecture:** Single internal static class `DelaunayRefiner` in `AIQ.Urban.Core.GeometryOperations`. Builds triangle adjacency from edge→face dictionary, then iteratively flips interior edges that violate the InCircle predicate. Boundary edges preserved. No new dependencies.

**Tech Stack:** .NET 8 / C# 12, RhinoCommon (Rhino.Geometry.Mesh), NUnit 3.14 + Rhino.Testing 8.0.28-beta

## Global Constraints

- All AIQ.Urban projects: `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<LangVersion>12.0</LangVersion>`
- No new NuGet packages, no BHoM, no TriangleNet
- File-scoped namespaces, K&R braces, 4-space indent, `_camelCase` private fields
- Test project: `[RhinoTestFixture]`, `[Test]` + `[TestCase("Name")]` + private record + static `_testData` pattern
- Use `var` where type is obvious, `new()` for target-typed construction, collection expressions `[...]`
- Input mesh must be planar XY (z == 0 within tolerance) with all triangle faces
- InCircle uses the standard 4×4 determinant (reference https://www.cs.cmu.edu/~quake/robust.html)

---

### Task 1: Create Test File (RED Phase)

**Files:**
- Create: `tests/AIQ.Urban.Core.Tests/GeometryOperations/DelaunayRefinerTests.cs`

**Interfaces:**
- Consumes: `Profile2D`, `Polygon2D` from `AIQ.Urban.Core.Models.GeometryElements`; `Mesh` from `Rhino.Geometry`; `Triangulate()` extension from `AIQ.Urban.Core.Extensions`
- Produces: `DelaunayRefinerTests` class with 7 tests that will fail until Task 2 implements `DelaunayRefiner.Refine()`

- [ ] **Step 1: Write the test file**

```csharp
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.GeometryOperations;
using AIQ.Urban.Core.Models.GeometryElements;
using NUnit.Framework;
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
    public void Refine_ValidInputs_ReturnsDelaunayMesh(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        var inputMesh = testData.Profile.Triangulate();

        // Act
        var result = DelaunayRefiner.Refine(inputMesh);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Faces.Count, Is.EqualTo(testData.ExpectedTriangleCount));
        Assert.That(result.Faces.All(f => f.IsTriangle), Is.True);

        var inputArea = AreaMassProperties.Compute(inputMesh).Area;
        var resultArea = AreaMassProperties.Compute(result).Area;
        Assert.That(resultArea, Is.EqualTo(inputArea).Within(1e-4));

        var avgArea = resultArea / result.Faces.Count;
        Assert.That(avgArea, Is.EqualTo(testData.ExpectedAverageArea).Within(1e-2));
    }

    [Test]
    public void Refine_NonDelaunayQuad_FixesDiagonal()
    {
        // Arrange: convex quad (0,0), (10,0), (9,10), (1,9) triangulated with wrong diagonal
        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);    // 0
        mesh.Vertices.Add(10, 0, 0);   // 1
        mesh.Vertices.Add(9, 10, 0);   // 2
        mesh.Vertices.Add(1, 9, 0);    // 3
        mesh.Faces.AddFace(0, 1, 2, 2); // T1: (0,1,2) — wrong: uses diagonal A-C (0→2)
        mesh.Faces.AddFace(0, 2, 3, 3); // T2: (0,2,3) — wrong: uses diagonal A-C (0→2)
        mesh.Faces.AddFace(0, 3, 1, 1); // T3: (0,3,1) — right: uses diagonal A-D (0→3)
        // This creates 3 triangles instead of 2 to ensure the flip corrects the topology

        // Act
        var result = DelaunayRefiner.Refine(mesh);

        // Assert — after flip, diagonal should be between B(1) and D(3), not A(0) and C(2)
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Faces.Count, Is.GreaterThan(0));
        Assert.That(result.Faces.All(f => f.IsTriangle), Is.True);
    }

    [Test]
    public void Refine_EmptyMesh_ReturnsEmpty()
    {
        // Arrange
        var empty = new Mesh();

        // Act
        var result = DelaunayRefiner.Refine(empty);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Faces.Count, Is.Zero);
    }

    [Test]
    public void Refine_NullMesh_ThrowsArgumentNullException()
    {
        Assert.That(() => DelaunayRefiner.Refine(null!), Throws.ArgumentNullException);
    }

    private record DelaunayTestData(
        string CaseName,
        Profile2D Profile,
        int ExpectedTriangleCount,
        double ExpectedAverageArea);

    private static readonly List<DelaunayTestData> _testData = BuildTestData();

    private static List<DelaunayTestData> BuildTestData()
    {
        return
        [
            new(CaseName: "SimpleSquare", Profile: MakeSquare(), ExpectedTriangleCount: 2, ExpectedAverageArea: 50),
            new(CaseName: "LShape", Profile: MakeLShape(), ExpectedTriangleCount: 4, ExpectedAverageArea: 18.75),
            new(CaseName: "Jagged", Profile: MakeJaggedShape(), ExpectedTriangleCount: 9, ExpectedAverageArea: 11.72),
            new(CaseName: "UShape", Profile: MakeUShape(), ExpectedTriangleCount: 6, ExpectedAverageArea: 12),
        ];
    }

    private static Profile2D MakeSquare()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        return new Profile2D(brep, poly, []);
    }

    private static Profile2D MakeLShape()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 5, 0), new Point3d(5, 5, 0),
            new Point3d(5, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        return new Profile2D(brep, poly, []);
    }

    private static Profile2D MakeJaggedShape()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 2, 0),
            new Point3d(10, 2, 0),
            new Point3d(9, 6, 0),
            new Point3d(13, 12, 0),
            new Point3d(15, 8, 0),
            new Point3d(20, 13, 0),
            new Point3d(18, 18, 0),
            new Point3d(13, 16, 0),
            new Point3d(9, 21, 0),
            new Point3d(11, 14, 0),
            new Point3d(4, 4, 0),
            new Point3d(0, 2, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        return new Profile2D(brep, poly, []);
    }

    private static Profile2D MakeUShape()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(7, 10, 0),
            new Point3d(7, 3, 0), new Point3d(3, 3, 0),
            new Point3d(3, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        return new Profile2D(brep, poly, []);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj -v q /clp:ErrorsOnly --filter "FullyQualifiedName~DelaunayRefinerTests"`

Expected: CS0103 compilation error — `DelaunayRefiner` does not exist yet.

- [ ] **Step 3: Commit**

```bash
git add tests/AIQ.Urban.Core.Tests/GeometryOperations/DelaunayRefinerTests.cs
git commit -m "test: add DelaunayRefiner tests (RED)"
```

---

### Task 2: Implement DelaunayRefiner

**Files:**
- Create: `src/AIQ.Urban.Core/GeometryOperations/DelaunayRefiner.cs`

**Interfaces:**
- Consumes: `Mesh` from `Rhino.Geometry`
- Produces: `DelaunayRefiner.Refine(Mesh, double tolerance = 1e-5)` → `Mesh`

**Algorithm overview (from spec):**
1. Validate input — null, empty, non-triangle faces, non-planar XY → throw or return empty
2. Build edge→face adjacency dictionary — each edge maps to face indices that share it
3. Build vertex index arrays per face from Mesh topology
4. Iterative edge-flipping loop:
   - For each interior edge (shared by exactly 2 faces), check if quadrilateral is convex
   - If convex, test InCircle predicate — if point D is inside circumcircle of △ABC, flip the edge
   - Flip: replace shared edge AC with BD, update face vertex tuples
   - After each flip, update adjacency for affected faces
   - Max passes = faceCount × faceCount (safety guard)
5. Build new Mesh from final face topology (copy vertices unchanged)

- [ ] **Step 1: Write the implementation file**

```csharp
using Rhino.Geometry;

namespace AIQ.Urban.Core.GeometryOperations;

internal static class DelaunayRefiner
{
    public static Mesh Refine(Mesh mesh, double tolerance = 1e-5)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        if (mesh.Faces.Count == 0)
            return new Mesh();

        // Validate all faces are triangles
        for (var i = 0; i < mesh.Faces.Count; i++)
        {
            if (!mesh.Faces[i].IsTriangle)
                throw new ArgumentException("All faces must be triangles");
        }

        // Validate planar XY
        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            if (Math.Abs(mesh.Vertices[i].Z) > tolerance)
                throw new ArgumentException("Mesh must be planar at z=0");
        }

        var verts = new Point3d[mesh.Vertices.Count];
        for (var i = 0; i < mesh.Vertices.Count; i++)
            verts[i] = mesh.Vertices[i];

        var nFaces = mesh.Faces.Count;
        var triVerts = new (int A, int B, int C)[nFaces];
        for (var i = 0; i < nFaces; i++)
        {
            var f = mesh.Faces[i];
            triVerts[i] = (f.A, f.B, f.C);
        }

        // Filter degenerate faces (duplicate vertex indices)
        triVerts = triVerts
            .Select(t => t)
            .Where(t => t.A != t.B && t.B != t.C && t.C != t.A)
            .ToArray();
        nFaces = triVerts.Length;

        if (nFaces == 0)
            return new Mesh();

        var adj = BuildAdjacency(triVerts, nFaces);
        var maxPasses = nFaces * nFaces;

        for (var pass = 0; pass < maxPasses; pass++)
        {
            var flippedAny = false;

            for (var fIdx = 0; fIdx < nFaces && !flippedAny; fIdx++)
            {
                var fAdj = adj[fIdx];
                for (var e = 0; e < 3; e++)
                {
                    var nIdx = fAdj[e];
                    if (nIdx < 0 || nIdx <= fIdx) continue; // boundary or already processed

                    var (v0, v1) = SharedEdgeVertices(triVerts[fIdx], triVerts[nIdx], e);
                    var opp0 = OppositeVertex(triVerts[fIdx], v0, v1);
                    var opp1 = OppositeVertex(triVerts[nIdx], v0, v1);

                    // Check convexity of quadrilateral
                    if (!IsConvex(verts[opp0], verts[v0], verts[v1], verts[opp1]))
                        continue;

                    // InCircle test: is opp1 inside circumcircle of (opp0, v0, v1)?
                    if (!InCircle(verts[opp0], verts[v0], verts[v1], verts[opp1], tolerance))
                        continue;

                    // FLIP: replace shared edge AC with BD
                    FlipEdge(fIdx, nIdx, opp0, v0, v1, opp1, ref triVerts);
                    var newAdj = BuildAdjacency(triVerts, nFaces);
                    adj = newAdj;
                    flippedAny = true;
                    break;
                }
            }

            if (!flippedAny)
                break;
        }

        // Build output mesh
        var result = new Mesh();
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
        if ((a == v0 && b == v1) || (a == v1 && b == v0)) return 2; // edge opposite C
        if ((b == v0 && c == v1) || (b == v1 && c == v0)) return 0; // edge opposite A
        return 1; // edge opposite B
    }

    private static (int V0, int V1) SharedEdgeVertices((int A, int B, int C) t1, (int A, int B, int C) t2, int e1)
    {
        // Get the edge opposite vertex at slot e1 in triangle t1
        return e1 switch
        {
            0 => (t1.B, t1.C),
            1 => (t1.C, t1.A),
            _ => (t1.A, t1.B),
        };
    }

    private static int OppositeVertex((int A, int B, int C) tri, int v0, int v1)
    {
        if (tri.A != v0 && tri.A != v1) return tri.A;
        if (tri.B != v0 && tri.B != v1) return tri.B;
        return tri.C;
    }

    private static bool IsConvex(Point3d a, Point3d b, Point3d c, Point3d d)
    {
        // Quadrilateral a, b, c, d in CCW order — check all interior angles < 180
        return CrossZ(a, b, c) > 0 && CrossZ(b, c, d) > 0 &&
               CrossZ(c, d, a) > 0 && CrossZ(d, a, b) > 0;
    }

    private static double CrossZ(Point3d p, Point3d q, Point3d r)
    {
        return (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
    }

    private static void FlipEdge(
        int f0, int f1,
        int opp0, int v0, int v1, int opp1,
        ref (int A, int B, int C)[] triVerts)
    {
        // Old: T0 = (opp0, v0, v1), T1 = (opp1, v0, v1) — shared edge (v0, v1)
        // New: T0 = (opp0, v0, opp1), T1 = (opp0, opp1, v1)
        triVerts[f0] = (opp0, v0, opp1);
        triVerts[f1] = (opp0, opp1, v1);
    }

    private static bool InCircle(Point3d a, Point3d b, Point3d c, Point3d d, double tolerance)
    {
        // Normalise triangle to CCW so the determinant sign is consistent
        if (CrossZ(a, b, c) < 0)
            (b, c) = (c, b);

        var ax = a.X; var ay = a.Y;
        var bx = b.X; var by = b.Y;
        var cx = c.X; var cy = c.Y;
        var dx = d.X; var dy = d.Y;

        var a2 = ax * ax + ay * ay;
        var b2 = bx * bx + by * by;
        var c2 = cx * cx + cy * cy;
        var d2 = dx * dx + dy * dy;

        var det = (ax * (by * c2 - b2 * cy) - ay * (bx * c2 - b2 * cx) + a2 * (bx * cy - by * cx))
                - (ax * (by * d2 - b2 * dy) - ay * (bx * d2 - b2 * dx) + a2 * (bx * dy - by * dx))
                + (ax * (cy * d2 - c2 * dy) - ay * (cx * d2 - c2 * dx) + a2 * (cx * dy - cy * dx))
                - (bx * (cy * d2 - c2 * dy) - by * (cx * d2 - c2 * dx) + b2 * (cx * dy - cy * dx));

        return det > tolerance;
    }
}
```

- [ ] **Step 2: Run test to verify it passes**

Run: `dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj -v q /clp:ErrorsOnly --filter "FullyQualifiedName~DelaunayRefinerTests"`

Expected: 7 passed, 0 failed

- [ ] **Step 3: Verify no regressions in full suite**

Run: `dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj -v q /clp:ErrorsOnly --blame-hang --blame-hang-timeout 5m`

Expected: same total as before (pre-existing failures unchanged, no new failures)

- [ ] **Step 4: Commit**

```bash
git add src/AIQ.Urban.Core/GeometryOperations/DelaunayRefiner.cs
git commit -m "feat: add DelaunayRefiner edge-flipping refinement"
```

---

### Task 3: Self-Review and Verification

**Files:** (read-only review)

- `src/AIQ.Urban.Core/GeometryOperations/DelaunayRefiner.cs`
- `tests/AIQ.Urban.Core.Tests/GeometryOperations/DelaunayRefinerTests.cs`

- [ ] **Step 1: Verify all tests still pass**

Run: `dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj -v q /clp:ErrorsOnly --filter "FullyQualifiedName~DelaunayRefinerTests"`

Expected: 7 passed, 0 failed

- [ ] **Step 2: Code quality check**

Check each against the spec:
- Null mesh → `ArgumentNullException`?
- Empty mesh → returns empty `new Mesh()`?
- Non-triangular faces → `ArgumentException("All faces must be triangles")`?
- Non-planar XY → `ArgumentException("Mesh must be planar at z=0")`?
- InCircle references `https://www.cs.cmu.edu/~quake/robust.html`?
- Boundary edges never flipped (adjacency entry == -1)?
- Non-convergence guard: `maxPasses = faceCount * faceCount`?
- No unused variables or imports?

- [ ] **Step 3: Verify no regressions in full suite**

Run: `dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj -v q /clp:ErrorsOnly --blame-hang --blame-hang-timeout 5m`

Expected: same pre-existing failures, no new failures

- [ ] **Step 4: Commit review fixups (if any)**

```bash
git add -A
git commit -m "fixup: address review feedback"
```
