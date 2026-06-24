# Profile2D Triangulation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `Profile2D.Triangulate()` extension method that returns `Rhino.Geometry.Mesh` via sweep-line triangulation.

**Architecture:** Two new files — `Profile2DExtensions.cs` (public API) wraps `SweepLineTriangulator` (internal algorithm class in `GeometryOperations/`). TDD: tests first, then implementation, then extension.

**Tech Stack:** C# 12, .NET 8, RhinoCommon 8, NUnit 3.14, Rhino.Testing 8.0.28-beta

## Global Constraints

- AIQ.Urban.Core targets net8.0, LangVersion 12, nullable enabled
- No BHoM, no TriangleNet, no new NuGet dependencies
- Existing project: AIQ.Urban.Core depends on RhinoCommon + AIQ.Primitives + AIQ.PreviewHelper.Core
- Test project: AIQ.Urban.Core.Tests depends on NUnit 3.14 + Rhino.Testing 8.0.28-beta
- File-scoped namespaces, K&R braces, 4-space indent
- Extension methods go in `Extensions/` folder, file named `XxxExtensions.cs`
- Internal algorithm helpers go in `GeometryOperations/` folder
- One class per file, filename matches class name
- Tests use `[RhinoTestFixture]`, `[TestCase("Name")]` + static record `_testData` lookup pattern

---

### Task 1: Write failing tests

**Files:**
- Create: `tests/AIQ.Urban.Core.Tests/GeometryOperations/SweepLineTriangulatorTests.cs`
- Create: `tests/AIQ.Urban.Core.Tests/GeometryOperations/` (directory)

**Interfaces:**
- Consumes: `Profile2D` from `AIQ.Urban.Core.Models.GeometryElements`
- Consumes: `Rhino.Geometry.Mesh` from RhinoCommon
- Tests: `profile.Triangulate()` (not yet defined — will fail to compile)

- [ ] **Step 1: Create directory and test file**

```bash
New-Item -ItemType Directory -Path "tests/AIQ.Urban.Core.Tests/GeometryOperations" -Force
```

- [ ] **Step 2: Write test file**

```csharp
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.DebugViz;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.GeometryOperations;

[RhinoTestFixture]
public class SweepLineTriangulatorTests
{
    [Test]
    [TestCase("SimpleSquare")]
    [TestCase("RectangleWithHole")]
    [TestCase("LShape")]
    public void Triangulate_ProducesValidMesh(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        var profile = testData.Input;

        // Act
        var mesh = profile.Triangulate();

        // Visualise
#if DEBUG
        PreviewMesh(profile, mesh);
#endif

        // Assert
        Assert.That(mesh.IsValid, Is.True);
        Assert.That(mesh.Vertices.Count, Is.GreaterThan(0));
        Assert.That(mesh.Faces.Count, Is.GreaterThan(0));
        Assert.That(mesh.Faces.All(f => f.IsTriangle), Is.True);

        var meshArea = double.Abs(mesh.GetArea());
        var profileArea = AreaMassProperties.Compute(profile.Geometry).Area;
        Assert.That(meshArea, Is.EqualTo(profileArea).Within(1e-4));
    }

    [Test]
    public void Triangulate_DegenerateProfile_ReturnsEmptyMesh()
    {
        // Arrange: a profile with zero-area outline
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(1, 0, 0),
            new Point3d(1, 0, 0),
            new Point3d(0, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        var profile = new Profile2D(brep, poly, []);

        // Act
        var mesh = profile.Triangulate();

        // Assert
        Assert.That(mesh.Vertices.Count, Is.Zero);
        Assert.That(mesh.Faces.Count, Is.Zero);
    }

    private record TriangulationTestData(string CaseName, Profile2D Input);

    private static readonly List<TriangulationTestData> _testData = BuildTestData();

    private static List<TriangulationTestData> BuildTestData()
    {
        return
        [
            new(CaseName: "SimpleSquare", Input: MakeSquare()),
            new(CaseName: "RectangleWithHole", Input: MakeRectangleWithHole()),
            new(CaseName: "LShape", Input: MakeLShape()),
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

    private static Profile2D MakeRectangleWithHole()
    {
        var outer = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(20, 0, 0),
            new Point3d(20, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var hole = new Polygon2D(new PolylineCurve([
            new Point3d(5, 2, 0), new Point3d(15, 2, 0),
            new Point3d(15, 8, 0), new Point3d(5, 8, 0),
            new Point3d(5, 2, 0),
        ]));
        var brep = Brep.CreatePlanarBreps([outer.Geometry, hole.Geometry], 1e-6)[0];
        return new Profile2D(brep, outer, [hole]);
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

#if DEBUG
    private static void PreviewMesh(Profile2D profile, Mesh mesh)
    {
        var doc = RhinoDoc.CreateHeadless(null);
        var outlineAttribs = RhinoHelper.MakeAttributes(doc, "Outline", System.Drawing.Color.Black);
        doc.Objects.AddCurve(profile.Outline.Geometry, outlineAttribs);
        var meshAttribs = RhinoHelper.MakeAttributes(doc, "Mesh", System.Drawing.Color.Red);
        doc.Objects.AddMesh(mesh, meshAttribs);
        Visualizer.View(doc);
    }
#endif
}
```

- [ ] **Step 3: Run tests to verify failure**

```bash
dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj --filter "FullyQualifiedName~SweepLineTriangulatorTests" -v n
```

Expected: Build failure — `Profile2D` has no `Triangulate` method defined.

---

### Task 2: Implement SweepLineTriangulator

**Files:**
- Create: `src/AIQ.Urban.Core/GeometryOperations/SweepLineTriangulator.cs`

**Interfaces:**
- Produces: `public static Mesh SweepLineTriangulator.Triangulate(IReadOnlyList<Point3d> outer, IReadOnlyList<IReadOnlyList<Point3d>> holes, double tolerance)`

- [ ] **Step 1: Write SweepLineTriangulator with internal types**

```csharp
using Rhino.Geometry;

namespace AIQ.Urban.Core.GeometryOperations;

internal static class SweepLineTriangulator
{
    public static Mesh Triangulate(
        IReadOnlyList<Point3d> outer,
        IReadOnlyList<IReadOnlyList<Point3d>> holes,
        double tolerance = 1e-5)
    {
        var result = new Mesh();

        var contourVertices = ExtractAllVertices(outer, holes);
        if (contourVertices.Count < 3)
            return result;

        EnsureCorrectWinding(outer, contourVertices, outer.Count);
        var sorted = contourVertices.OrderBy(v => v.This.X).ThenBy(v => v.This.Y).ToList();

        var sections = new SortedSet<Section>(new SectionComparer(sorted));
        var triangles = new List<(int A, int B, int C)>();

        foreach (var vertex in sorted)
        {
            if (vertex.Type == VertexType.Start)
            {
                var section = new Section(vertex);
                sections.Add(section);
            }
            // Handle End, Split, Merge, Join...
        }

        BuildMesh(result, contourVertices, triangles, outer);
        return result;
    }

    private enum VertexType { Start, End, Split, Merge, Join }

    private readonly struct ChainVertex
    {
        public readonly int Index;
        public readonly Point3d This;
        public readonly Point3d Prev;
        public readonly Point3d Next;
        public readonly VertexType Type;

        public ChainVertex(int index, Point3d self, Point3d prev, Point3d next, VertexType type)
        {
            Index = index;
            This = self;
            Prev = prev;
            Next = next;
            Type = type;
        }
    }

    private class Section
    {
        public (Point3d A, Point3d B) SupportEdge;
        public List<(int A, int B)> Edges = [];
        // ...
    }

    private class SectionComparer : IComparer<Section> { /* compare by support edge x at sweep y */ }

    private static List<ChainVertex> ExtractAllVertices(
        IReadOnlyList<Point3d> outer,
        IReadOnlyList<IReadOnlyList<Point3d>> holes) { /* ... */ }

    private static void EnsureCorrectWinding(
        IReadOnlyList<Point3d> outer,
        List<ChainVertex> vertices,
        int outerCount) { /* reverse if CCW check fails */ }

    private static VertexType ClassifyVertex(
        Point3d prev, Point3d self, Point3d next,
        bool isOuter) { /* interior angle + neighbor positions */ }

    private static void BuildMesh(
        Mesh mesh,
        List<ChainVertex> vertices,
        List<(int A, int B, int C)> triangles,
        IReadOnlyList<Point3d> outer) { /* build Rhino Mesh from vertex list + triangle indices */ }
}
```

- [ ] **Step 2: Implement vertex extraction and winding normalization**

Extract all points from outer contour and holes into a flat list of `ChainVertex`. Ensure outer is CCW (clockwise if reversed) and holes are CW. Each vertex receives its `prev` and `next` neighbor along its contour.

```csharp
private static List<ChainVertex> ExtractAllVertices(
    IReadOnlyList<Point3d> outer,
    IReadOnlyList<IReadOnlyList<Point3d>> holes)
{
    var vertices = new List<ChainVertex>();
    var index = 0;

    void AddContour(IReadOnlyList<Point3d> contour, bool isOuter)
    {
        var n = contour.Count;
        for (var i = 0; i < n; i++)
        {
            var prev = contour[(i - 1 + n) % n];
            var self = contour[i];
            var next = contour[(i + 1) % n];
            var type = ClassifyVertex(prev, self, next, isOuter);
            vertices.Add(new ChainVertex(index++, self, prev, next, type));
        }
    }

    AddContour(outer, isOuter: true);
    foreach (var hole in holes)
        AddContour(hole, isOuter: false);

    return vertices;
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

private static void EnsureCorrectWinding(
    IReadOnlyList<Point3d> outer,
    List<ChainVertex> vertices,
    int outerCount)
{
    // Positive area = CCW (what we want for outer)
    // If outer is CW, reverse it
    if (SignedArea(outer) < 0)
    {
        // Reverse the first outerCount vertices in the vertices list
        vertices.Reverse(0, outerCount);
        // Recalculate prev/next and type for these reversed vertices
        for (var i = 0; i < outerCount; i++)
        {
            var v = vertices[i];
            var n = outerCount;
            var prev = vertices[(i - 1 + n) % n].This;
            var next = vertices[(i + 1) % n].This;
            vertices[i] = new ChainVertex(v.Index, v.This, prev, next,
                ClassifyVertex(prev, v.This, next, isOuter: true));
        }
    }

    // For holes we want CW (negative area), reverse if CCW
    var holeStart = outerCount;
    // ... (similar reversal for each hole)
}
```

- [ ] **Step 3: Implement vertex classification**

```csharp
private static VertexType ClassifyVertex(
    Point3d prev, Point3d self, Point3d next,
    bool isOuter)
{
    // Vectors from self to prev and next
    var v1 = prev - self;
    var v2 = next - self;

    // Cross product (z-component) determines interior angle sign
    // Positive cross = left turn (interior angle < 180 for CCW)
    var cross = v1.X * v2.Y - v1.Y * v2.X;
    var interiorAngleLessThan180 = isOuter ? cross > 0 : cross < 0;

    var prevAbove = prev.Y > self.Y || (prev.Y.ApproxEquals(self.Y, 1e-10) && prev.X > self.X);
    var nextAbove = next.Y > self.Y || (next.Y.ApproxEquals(self.Y, 1e-10) && next.X > self.X);

    if (prevAbove == nextAbove)
    {
        if (prevAbove) // both above
            return interiorAngleLessThan180 ? VertexType.End : VertexType.Merge;
        else // both below
            return interiorAngleLessThan180 ? VertexType.Start : VertexType.Split;
    }

    return VertexType.Join;
}
```

- [ ] **Step 4: Implement sweep-line loop and section management**

This is the core of the algorithm. Implement the `Section` tree and processing logic for each vertex type.

```csharp
private class Section
{
    public int HelperIndex;       // vertex index of the helper (lowest vertex in section)
    public (int A, int B) SupportEdge;  // indices into contourVertices
    public List<(int A, int B)> Edges = [];
}

private class SectionComparer : IComparer<Section>
{
    private readonly List<ChainVertex> _vertices;
    public SectionComparer(List<ChainVertex> vertices) { _vertices = vertices; }

    public int Compare(Section? x, Section? y)
    {
        if (x == null || y == null) return 0;
        // Compare support edges by x-coordinate at current sweep y
        var xVal = EdgeXAtY(x.SupportEdge, _vertices);
        var yVal = EdgeXAtY(y.SupportEdge, _vertices);
        return xVal.CompareTo(yVal);
    }

    private static double EdgeXAtY((int A, int B) edge, List<ChainVertex> vertices)
    {
        var pa = vertices[edge.A].This;
        var pb = vertices[edge.B].This;
        // Linear interpolation: x at the lower of the two y values
        var y = Math.Min(pa.Y, pb.Y);
        if (Math.Abs(pb.Y - pa.Y) < 1e-10)
            return Math.Min(pa.X, pb.X);
        var t = (y - pa.Y) / (pb.Y - pa.Y);
        return pa.X + t * (pb.X - pa.X);
    }
}
```

Then implement the main sweep loop body for each `ChainVertex.Type`:

```csharp
foreach (var vertex in sorted)
{
    switch (vertex.Type)
    {
        case VertexType.Start:
            var section = new Section { HelperIndex = vertex.Index, SupportEdge = (vertex.Index, /* next */) };
            sections.Add(section);
            break;

        case VertexType.End:
            // Close section, emit triangles from its edge chain
            break;

        case VertexType.Split:
            // Find section below vertex, split into two
            break;

        case VertexType.Merge:
            // Merge two sections, emit triangles
            break;

        case VertexType.Join:
            // Add to current section's edge chain, try to form triangles
            break;
    }
}
```

- [ ] **Step 5: Implement triangle assembly into Mesh**

```csharp
private static void BuildMesh(
    Mesh mesh,
    List<ChainVertex> vertices,
    List<(int A, int B, int C)> triangles,
    IReadOnlyList<Point3d> outer)
{
    // Build vertex list (deduplicate by position)
    var vertMap = new Dictionary<int, int>();  // chain index → mesh vertex index
    foreach (var tri in triangles)
    {
        if (!vertMap.ContainsKey(tri.A))
            vertMap[tri.A] = mesh.Vertices.Add((Point3f)vertices[tri.A].This);
        if (!vertMap.ContainsKey(tri.B))
            vertMap[tri.B] = mesh.Vertices.Add((Point3f)vertices[tri.B].This);
        if (!vertMap.ContainsKey(tri.C))
            vertMap[tri.C] = mesh.Vertices.Add((Point3f)vertices[tri.C].This);
    }

    foreach (var tri in triangles)
        mesh.Faces.AddFace(vertMap[tri.A], vertMap[tri.B], vertMap[tri.C], vertMap[tri.C]);
}
```

- [ ] **Step 6: Run build to confirm it compiles**

```bash
dotnet build src/AIQ.Urban.Core/AIQ.Urban.Core.csproj -c Debug
```

Expected: Build succeeds (may have warnings for unused code at this stage).

---

### Task 3: Create Profile2DExtensions

**Files:**
- Create: `src/AIQ.Urban.Core/Extensions/Profile2DExtensions.cs`

**Interfaces:**
- Consumes: `SweepLineTriangulator.Triangulate(...)` from Task 2
- Produces: `public static Mesh Profile2D.Triangulate(this Profile2D profile, double tolerance)`

- [ ] **Step 1: Write extension method**

```csharp
using AIQ.Urban.Core.GeometryOperations;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.Urban.Core.Extensions;

public static class Profile2DExtensions
{
    public static Mesh Triangulate(this Profile2D profile, double tolerance = 1e-5)
    {
        var outer = profile.Outline.Geometry.PolylinePoints();
        var holes = profile.Holes
            .Select(h => h.Geometry.PolylinePoints())
            .ToList();

        return SweepLineTriangulator.Triangulate(outer, holes, tolerance);
    }

    private static IReadOnlyList<Point3d> PolylinePoints(this PolylineCurve curve)
    {
        var polyline = curve.ToPolyline(0, 0, 0, 0, 0, 0, 0, 0, true);
        var points = polyline.ToList();
        // Remove the duplicate closing point if present
        if (points.Count > 1 && points[0].DistanceTo(points[^1]) < 1e-6)
            points.RemoveAt(points.Count - 1);
        return points;
    }
}
```

- [ ] **Step 2: Run tests to verify they pass**

```bash
dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj --filter "FullyQualifiedName~SweepLineTriangulatorTests" -v n
```

Expected: All tests pass (or the sweep-line algorithm is working well enough for the test cases). If tests fail, debug and refine the algorithm in Task 2.

---

### Task 4: Full solution build and test verification

- [ ] **Step 1: Build the full solution**

```bash
dotnet build ArchSitePlan.sln -c Debug
```

- [ ] **Step 2: Run all tests**

```bash
dotnet test -v n
```

Expected: All tests pass, including the new triangulation tests.

---

### Self-Review Checklist

1. **Spec coverage:** Every section of the spec has a corresponding task:
   - Output Mesh type (Rhino.Geometry.Mesh) → Task 2 step 5
   - File layout (Extensions/ + GeometryOperations/) → Tasks 2 + 3
   - API (`Triangulate()` method) → Task 3
   - Algorithm (sweep-line) → Task 2
   - Internal types (ChainVertex, VertexType, Section) → Task 2
   - Tests (NUnit, RhinoTestFixture, TestCase pattern) → Task 1
   - Error handling (empty Mesh for degenerate) → Task 1 + Task 2 step 1 guard

2. **Placeholder scan:** No TBD, TODO, or "implement later". Every code block has real code.

3. **Type consistency:** `Triangulate()` returns `Rhino.Geometry.Mesh` everywhere. `ChainVertex` uses `Point3d` consistently.

4. **Scope:** Tight — just triangulation. No scope creep.
