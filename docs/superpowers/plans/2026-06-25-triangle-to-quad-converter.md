# Triangle-to-Quad Converter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `TriangleToQuadConverter.Convert(Mesh)` (internal) and `Mesh.ToQuadMesh()` (public extension) that greedily merges adjacent triangle pairs into quad faces using a diagonal-ratio quality metric.

**Architecture:** Internal static class `TriangleToQuadConverter` with a single `Convert(Mesh)` entry point, plus a public `MeshExtensions.ToQuadMesh()` extension method wrapper. Builds edge→triangle adjacency dictionary, then iterates triangles in order, scoring unmerged neighbors by diagonal ratio (`max(d1,d2)/min(d1,d2)`), and merges the best-scoring pair. Unmergeable triangles remain as-is.

**Tech Stack:** .NET 8 / C# 12, RhinoCommon (Rhino.Geometry.Mesh), NUnit 3.14 + Rhino.Testing 8.0.28-beta

## Global Constraints

- All AIQ.Urban projects: `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<LangVersion>12.0</LangVersion>`
- No new NuGet packages
- File-scoped namespaces, K&R braces, 4-space indent, `_camelCase` private fields
- Test project: `[RhinoTestFixture]`, `[Test]` + `[TestCase("Name")]` + private record + static `_testData` + `BuildTestData()` pattern
- Tests follow AAA comments (`// Arrange`, `// Act`, `// Visualise` inside `#if DEBUG`, `// Assert`)
- Use `var` where type is obvious, `new()` for target-typed construction, collection expressions `[...]`
- Input mesh must be planar XY (z == 0) with all triangle faces
- No nested loops deeper than 2 levels — extract helper methods
- Descriptive but concise naming, no comments in implementation code

---

### Task 1: Create Test File (RED Phase)

**Files:**
- Create: `tests/AIQ.Urban.Core.Tests/GeometryOperations/TriangleToQuadConverterTests.cs`

**Interfaces:**
- Consumes: `Mesh` from `Rhino.Geometry`; `ToQuadMesh()` extension from `AIQ.Urban.Core.Extensions`
- Produces: `TriangleToQuadConverterTests` class with 10 tests that will fail until Task 2 implements the converter

- [ ] **Step 1: Write the test file**

```csharp
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.GeometryOperations;
using AIQ.Urban.DebugViz;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.GeometryOperations;

[RhinoTestFixture]
public class TriangleToQuadConverterTests
{
    [Test]
    [TestCase("TwoTrianglesSquare")]
    [TestCase("SixTrianglesGrid")]
    [TestCase("FanTriangles")]
    [TestCase("LShape")]
    [TestCase("UShape")]
    public void Convert_ProducesValidMixedMesh(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        var input = testData.Input;

        // Act
        var result = input.ToQuadMesh();

        // Visualise
#if DEBUG
        PreviewMesh(testName, result, input);
#endif

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Vertices.Count, Is.EqualTo(input.Vertices.Count));
        Assert.That(result.Faces.Count, Is.EqualTo(testData.ExpectedFaceCount));

        var inputArea = AreaMassProperties.Compute(input).Area;
        var resultArea = AreaMassProperties.Compute(result).Area;
        Assert.That(resultArea, Is.EqualTo(inputArea).Within(1e-4));
    }

    [Test]
    public void Convert_EmptyMesh_ReturnsEmptyMesh()
    {
        // Arrange
        var input = new Mesh();

        // Act
        var result = input.ToQuadMesh();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Vertices.Count, Is.Zero);
        Assert.That(result.Faces.Count, Is.Zero);
    }

    [Test]
    public void Convert_SingleTriangle_ReturnsSameTriangle()
    {
        // Arrange
        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);
        mesh.Vertices.Add(10, 0, 0);
        mesh.Vertices.Add(5, 10, 0);
        mesh.Faces.AddFace(0, 1, 2, 2);

        // Act
        var result = mesh.ToQuadMesh();

        // Assert
        Assert.That(result.Vertices.Count, Is.EqualTo(3));
        Assert.That(result.Faces.Count, Is.EqualTo(1));
        Assert.That(result.Faces[0].IsTriangle, Is.True);
    }

    [Test]
    public void Convert_NonTriangularFaces_ThrowsArgumentException()
    {
        // Arrange
        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);
        mesh.Vertices.Add(10, 0, 0);
        mesh.Vertices.Add(10, 10, 0);
        mesh.Vertices.Add(0, 10, 0);
        mesh.Faces.AddFace(0, 1, 2, 3);

        // Act & Assert
        Assert.That(() => mesh.ToQuadMesh(), Throws.ArgumentException);
    }

    [Test]
    public void Convert_NullMesh_ThrowsArgumentNullException()
    {
        Mesh mesh = null!;

        Assert.That(() => mesh.ToQuadMesh(), Throws.ArgumentNullException);
    }

    private record ConverterTestData(
        string CaseName,
        Mesh Input,
        int ExpectedFaceCount);

    private static readonly List<ConverterTestData> _testData = BuildTestData();

    private static List<ConverterTestData> BuildTestData()
    {
        return
        [
            new(CaseName: "TwoTrianglesSquare",
                Input: MakeTwoTriangleSquare(),
                ExpectedFaceCount: 1),
            new(CaseName: "SixTrianglesGrid",
                Input: MakeSixTriangleGrid(),
                ExpectedFaceCount: 3),
            new(CaseName: "FanTriangles",
                Input: MakeFanTriangles(),
                ExpectedFaceCount: 6),
            new(CaseName: "LShape",
                Input: MakeLShapeMesh(),
                ExpectedFaceCount: 2),
            new(CaseName: "UShape",
                Input: MakeUShapeMesh(),
                ExpectedFaceCount: 4),
        ];
    }

    private static Mesh MakeTwoTriangleSquare()
    {
        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);  // 0
        mesh.Vertices.Add(10, 0, 0); // 1
        mesh.Vertices.Add(10, 10, 0);// 2
        mesh.Vertices.Add(0, 10, 0); // 3
        mesh.Faces.AddFace(0, 1, 2, 2);
        mesh.Faces.AddFace(0, 2, 3, 3);
        return mesh;
    }

    private static Mesh MakeSixTriangleGrid()
    {
        // 2 columns x 3 rows of triangles forming 3 quads
        // Vertices: 0=(0,0), 1=(10,0), 2=(20,0),
        //           3=(0,10), 4=(10,10), 5=(20,10),
        //           6=(0,20), 7=(10,20), 8=(20,20)
        // Triangles: (0,1,4),(0,4,3) | (1,2,5),(1,5,4) | (3,4,7),(3,7,6)
        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);
        mesh.Vertices.Add(10, 0, 0);
        mesh.Vertices.Add(20, 0, 0);
        mesh.Vertices.Add(0, 10, 0);
        mesh.Vertices.Add(10, 10, 0);
        mesh.Vertices.Add(20, 10, 0);
        mesh.Vertices.Add(0, 20, 0);
        mesh.Vertices.Add(10, 20, 0);
        mesh.Vertices.Add(20, 20, 0);
        mesh.Faces.AddFace(0, 1, 4, 4);
        mesh.Faces.AddFace(0, 4, 3, 3);
        mesh.Faces.AddFace(1, 2, 5, 5);
        mesh.Faces.AddFace(1, 5, 4, 4);
        mesh.Faces.AddFace(3, 4, 7, 7);
        mesh.Faces.AddFace(3, 7, 6, 6);
        return mesh;
    }

    private static Mesh MakeFanTriangles()
    {
        // 6 triangles all sharing vertex 0 (a fan)
        // Vertices form a hexagon around center
        // None should merge since no two triangles share a full edge
        var mesh = new Mesh();
        mesh.Vertices.Add(10, 10, 0); // 0 - center
        mesh.Vertices.Add(0, 10, 0);  // 1
        mesh.Vertices.Add(5, 18, 0);  // 2
        mesh.Vertices.Add(15, 18, 0); // 3
        mesh.Vertices.Add(20, 10, 0); // 4
        mesh.Vertices.Add(15, 2, 0);  // 5
        mesh.Vertices.Add(5, 2, 0);   // 6
        mesh.Faces.AddFace(0, 1, 2, 2);
        mesh.Faces.AddFace(0, 2, 3, 3);
        mesh.Faces.AddFace(0, 3, 4, 4);
        mesh.Faces.AddFace(0, 4, 5, 5);
        mesh.Faces.AddFace(0, 5, 6, 6);
        mesh.Faces.AddFace(0, 6, 1, 1);
        return mesh;
    }

    private static Mesh MakeLShapeMesh()
    {
        // L-shape: (0,0)-(10,0)-(10,5)-(5,5)-(5,10)-(0,10)
        // 4 triangles from sweep-line triangulation
        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);
        mesh.Vertices.Add(10, 0, 0);
        mesh.Vertices.Add(10, 5, 0);
        mesh.Vertices.Add(5, 5, 0);
        mesh.Vertices.Add(5, 10, 0);
        mesh.Vertices.Add(0, 10, 0);
        mesh.Faces.AddFace(0, 1, 3, 3);
        mesh.Faces.AddFace(1, 2, 3, 3);
        mesh.Faces.AddFace(0, 3, 5, 5);
        mesh.Faces.AddFace(3, 4, 5, 5);
        return mesh;
    }

    private static Mesh MakeUShapeMesh()
    {
        // U-shape: (0,0)-(10,0)-(10,10)-(7,10)-(7,3)-(3,3)-(3,10)-(0,10)
        // 6 triangles from sweep-line triangulation
        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);
        mesh.Vertices.Add(10, 0, 0);
        mesh.Vertices.Add(10, 10, 0);
        mesh.Vertices.Add(7, 10, 0);
        mesh.Vertices.Add(7, 3, 0);
        mesh.Vertices.Add(3, 3, 0);
        mesh.Vertices.Add(3, 10, 0);
        mesh.Vertices.Add(0, 10, 0);
        mesh.Faces.AddFace(0, 1, 5, 5);
        mesh.Faces.AddFace(1, 4, 5, 5);
        mesh.Faces.AddFace(1, 2, 4, 4);
        mesh.Faces.AddFace(2, 3, 4, 4);
        mesh.Faces.AddFace(0, 5, 7, 7);
        mesh.Faces.AddFace(5, 6, 7, 7);
        return mesh;
    }

#if DEBUG
    private static void PreviewMesh(string name, Mesh result, Mesh input)
    {
        var doc = RhinoDoc.CreateHeadless(null);
        var inputAttribs = RhinoHelper.MakeAttributes(doc, $"{name}::Input", Color.LightGray);
        var inputCopy = input.DuplicateMesh();
        inputCopy.Translate(0, 0, -0.1);
        doc.Objects.AddMesh(inputCopy, inputAttribs);

        var resultAttribs = RhinoHelper.MakeAttributes(doc, $"{name}::Result", Color.CornflowerBlue);
        doc.Objects.AddMesh(result, resultAttribs);

        var quadEdgesAttribs = RhinoHelper.MakeAttributes(doc, $"{name}::Quads", Color.DarkOrange);
        var edgeCount = result.TopologyEdges.Count;
        for (var i = 0; i < edgeCount; i++)
            doc.Objects.AddCurve(result.TopologyEdges.EdgeLine(i).ToNurbsCurve(), quadEdgesAttribs);

        Visualizer.View(doc);
    }
#endif
}
```

- [ ] **Step 2: Verify it fails**

```bash
dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj --filter "FullyQualifiedName~TriangleToQuadConverterTests" -v n
```

Expected: Build fails because `ToQuadMesh()` doesn't exist yet

- [ ] **Step 3: Commit**

```bash
git add tests/AIQ.Urban.Core.Tests/GeometryOperations/TriangleToQuadConverterTests.cs
git commit -m "test: add TriangleToQuadConverterTests (RED)"
```

---

### Task 2: Implement Converter (GREEN Phase)

**Files:**
- Create: `src/AIQ.Urban.Core/GeometryOperations/TriangleToQuadConverter.cs`
- Create: `src/AIQ.Urban.Core/Extensions/MeshExtensions.cs`

**Interfaces:**
- Consumes: `Mesh` from `Rhino.Geometry`
- Produces: `TriangleToQuadConverter.Convert(Mesh)` → `Mesh` and `Mesh.ToQuadMesh()` → `Mesh`

- [ ] **Step 1: Write the converter class**

Create `src/AIQ.Urban.Core/GeometryOperations/TriangleToQuadConverter.cs`:

```csharp
using Rhino.Geometry;

namespace AIQ.Urban.Core.GeometryOperations;

internal static class TriangleToQuadConverter
{
    public static Mesh Convert(Mesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        if (mesh.Faces.Count == 0)
            return new Mesh();

        if (mesh.Faces.Any(f => !f.IsTriangle))
            throw new ArgumentException("All faces must be triangles");

        if (mesh.Vertices.Any(v => Math.Abs(v.Z) > 1e-6))
            throw new ArgumentException("Mesh must be planar at z=0");

        if (mesh.Faces.Count <= 2)
            return mesh.DuplicateMesh();

        var edgeToTris = BuildEdgeMap(mesh);
        var neighbors = BuildNeighborLists(mesh.Faces.Count, edgeToTris);
        var merged = new bool[mesh.Faces.Count];
        var quads = new List<(int, int, int, int)>();
        var leftoverTris = new List<(int, int, int)>();

        for (var i = 0; i < mesh.Faces.Count; i++)
        {
            if (merged[i])
                continue;

            var bestPair = FindBestMergePair(i, mesh, neighbors, merged);

            if (bestPair.HasValue)
            {
                var (neighborIdx, uA, s0, s1, uB) = bestPair.Value;
                quads.Add((uA, s0, uB, s1));
                merged[i] = true;
                merged[neighborIdx] = true;
            }
            else
            {
                var f = mesh.Faces[i];
                leftoverTris.Add((f.A, f.B, f.C));
            }
        }

        return BuildResultMesh(mesh, quads, leftoverTris);
    }

    private static Dictionary<(int, int), List<int>> BuildEdgeMap(Mesh mesh)
    {
        var map = new Dictionary<(int, int), List<int>>();

        for (var i = 0; i < mesh.Faces.Count; i++)
        {
            var f = mesh.Faces[i];
            AddEdge(map, i, f.A, f.B);
            AddEdge(map, i, f.B, f.C);
            AddEdge(map, i, f.C, f.A);
        }

        return map;
    }

    private static void AddEdge(Dictionary<(int, int), List<int>> map, int faceIdx, int v0, int v1)
    {
        var key = v0 < v1 ? (v0, v1) : (v1, v0);

        if (!map.TryGetValue(key, out var list))
        {
            list = [];
            map[key] = list;
        }

        list.Add(faceIdx);
    }

    private static List<int>[] BuildNeighborLists(int faceCount, Dictionary<(int, int), List<int>> edgeToTris)
    {
        var neighbors = new List<int>[faceCount];

        for (var i = 0; i < faceCount; i++)
            neighbors[i] = [];

        foreach (var kvp in edgeToTris)
        {
            if (kvp.Value.Count != 2)
                continue;

            var t0 = kvp.Value[0];
            var t1 = kvp.Value[1];
            neighbors[t0].Add(t1);
            neighbors[t1].Add(t0);
        }

        return neighbors;
    }

    private static (int neighborIdx, int uA, int s0, int s1, int uB)? FindBestMergePair(
        int triIdx, Mesh mesh, List<int>[] neighbors, bool[] merged)
    {
        var tri = mesh.Faces[triIdx];
        var bestScore = double.MaxValue;
        (int, int, int, int, int)? bestPair = null;

        foreach (var neighborIdx in neighbors[triIdx])
        {
            if (merged[neighborIdx])
                continue;

            if (TryGetSharedEdge(tri, mesh.Faces[neighborIdx], out var s0, out var s1, out var uA, out var uB))
            {
                if (!IsQuadConvex(mesh, uA, s0, uB, s1))
                    continue;

                var d1 = mesh.Vertices[s0].DistanceTo(mesh.Vertices[s1]);
                var d2 = mesh.Vertices[uA].DistanceTo(mesh.Vertices[uB]);
                var min = Math.Min(d1, d2);
                var max = Math.Max(d1, d2);

                if (min < 1e-10)
                    continue;

                var score = max / min;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestPair = (neighborIdx, uA, s0, s1, uB);
                }
            }
        }

        return bestPair;
    }

    private static bool TryGetSharedEdge(
        MeshFace a, MeshFace b,
        out int s0, out int s1,
        out int uA, out int uB)
    {
        var aVerts = new[] { a.A, a.B, a.C };
        var bVerts = new[] { b.A, b.B, b.C };

        s0 = -1;
        s1 = -1;
        uA = -1;
        uB = -1;
        var sharedCount = 0;

        foreach (var va in aVerts)
        {
            if (bVerts.Contains(va))
            {
                if (sharedCount == 0)
                    s0 = va;
                else
                    s1 = va;
                sharedCount++;
            }
            else
            {
                uA = va;
            }
        }

        foreach (var vb in bVerts)
        {
            if (!aVerts.Contains(vb))
                uB = vb;
        }

        return sharedCount == 2;
    }

    private static bool IsQuadConvex(Mesh mesh, int a, int b, int c, int d)
    {
        var pa = mesh.Vertices[a];
        var pb = mesh.Vertices[b];
        var pc = mesh.Vertices[c];
        var pd = mesh.Vertices[d];

        var area = 0.5 * (
            pa.X * pb.Y - pb.X * pa.Y +
            pb.X * pc.Y - pc.X * pb.Y +
            pc.X * pd.Y - pd.X * pc.Y +
            pd.X * pa.Y - pa.X * pd.Y);

        return area > 1e-10;
    }

    private static Mesh BuildResultMesh(
        Mesh source,
        List<(int, int, int, int)> quads,
        List<(int, int, int)> tris)
    {
        var result = new Mesh();
        result.Vertices.AddVertices(source.Vertices);

        foreach (var (a, b, c, d) in quads)
            result.Faces.AddFace(a, b, c, d);

        foreach (var (a, b, c) in tris)
            result.Faces.AddFace(a, b, c, c);

        result.Normals.ComputeNormals();
        result.Compact();

        return result;
    }
}
```

- [ ] **Step 2: Write the extension method**

Create `src/AIQ.Urban.Core/Extensions/MeshExtensions.cs`:

```csharp
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
```

- [ ] **Step 3: Run tests to verify they pass**

```bash
dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj --filter "FullyQualifiedName~TriangleToQuadConverterTests" -v n
```

Expected: All 7 tests pass (5 parametrized + 3 explicit)

- [ ] **Step 4: Run full test suite to check for regressions**

```bash
dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj -v n
```

Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add src/AIQ.Urban.Core/GeometryOperations/TriangleToQuadConverter.cs
git add src/AIQ.Urban.Core/Extensions/MeshExtensions.cs
git commit -m "feat: add TriangleToQuadConverter and Mesh.ToQuadMesh() extension"
```

---

### Task 3: Build & Verify

**Files:**
- Verify: `ArchSitePlan.sln` builds cleanly with all project configurations

- [ ] **Step 1: Build the full solution**

```bash
dotnet build ArchSitePlan.sln -c Debug_NoViz
```

Expected: Build succeeds with no errors

- [ ] **Step 2: Run full test suite**

```bash
dotnet test -v n
```

Expected: All tests pass

- [ ] **Step 3: Final commit if any build/test fixes were needed**

```bash
git add -A
git commit -m "fix: address build/test issues from triangle-to-quad converter"
```
