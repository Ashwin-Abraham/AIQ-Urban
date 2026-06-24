# Profile2D Triangulation — Design Spec

## Purpose

Add a `Triangulate()` extension method to `Profile2D` that produces a `Rhino.Geometry.Mesh` from any simple planar polygon (with or without holes). This enables downstream consumers to work with a tessellated representation of FloorPlates, Parcels, TransitSurfaces, and other `Profile2D`-based geometry.

## Output Mesh

- **Type:** `Rhino.Geometry.Mesh`
- **Vertices:** `List<Point3f>` in XY plane (z=0)
- **Faces:** All triangles via `MeshFace(A, B, C, C)` (Rhino convention)
- **Normals:** Not computed (caller calls `Mesh.UnifyNormals()` if needed)
- **Texture coords:** Not set

## File Layout

```
src/AIQ.Urban.Core/
  Extensions/
    Profile2DExtensions.cs          — public extension method
  GeometryOperations/
    SweepLineTriangulator.cs        — full algorithm implementation
```

### Namespaces
- `AIQ.Urban.Core.Extensions` — Profile2DExtensions
- `AIQ.Urban.Core.GeometryOperations` — SweepLineTriangulator

## API

```csharp
public static Mesh Triangulate(this Profile2D profile, double tolerance = 1e-5)
```

Returns empty `Mesh` (zero faces, zero vertices) for degenerate profiles.

## Algorithm

Sweep-line triangulation based on Nail Sharipov's approach (https://dev.to/nail_sharipov_5d810d8cf71/monotone-triangulation-practical-advice-1k4j):

1. **Validate input** — ensure CCW outer contour, CW holes; reverse if needed. Reject self-intersections.
2. **Build chain vertices** — each vertex linked to prev/next in its contour
3. **Classify vertices** — Start, End, Split, Merge, Join based on interior angle and neighbor positions
4. **Sweep** — process vertices sorted by x→y, maintain balanced tree of active Sections
5. **Triangulate on-the-fly** — form triangles as vertices are processed, using the active section's edge chain
6. **Assemble Mesh** — collect all triangle faces and vertices into `Rhino.Geometry.Mesh`

### Internal types (in SweepLineTriangulator.cs)
- `ChainVertex` — index, this/next/prev points
- `VertexType` — enum (Start, End, Split, Merge, Join)
- `Section` — monotone polygon fragment with support edge + edge chain

## Testing (AIQ.Urban.Core.Tests)

**Framework:** NUnit 3.14 + `[RhinoTestFixture]`

**Pattern:** `[TestCase("CaseName")]` + private record + static `_testData` via `BuildTestData()`, matching existing conventions in `LongEdgeToCenterTests`, `AddBuildingStepTests`, etc.

```csharp
[RhinoTestFixture]
public class SweepLineTriangulatorTests
{
    [Test]
    [TestCase("SimpleSquare")]
    [TestCase("RectangleWithHole")]
    [TestCase("LShape")]
    [TestCase("ComplexWithMultipleHoles")]
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

        var meshArea = double.Abs(mesh.GetArea());  // Mesh area may be signed
        var profileArea = AreaMassProperties.Compute(profile.Geometry).Area;
        Assert.That(meshArea, Is.EqualTo(profileArea).Within(1e-4));
    }

    [Test]
    public void Triangulate_DegenerateProfile_ReturnsEmptyMesh()
    {
        var profile = /* degenerate */;
        var mesh = profile.Triangulate();
        Assert.That(mesh.Vertices.Count, Is.Zero);
        Assert.That(mesh.Faces.Count, Is.Zero);
    }
}
```

**Test data helpers:**

Geometry factory methods follow `LongEdgeToCenterTests` convention — one helper per shape:

```csharp
private record TriangulationTestData(string CaseName, Profile2D Input);

private static readonly List<TriangulationTestData> _testData = BuildTestData();

private static List<TriangulationTestData> BuildTestData()
{
    return
    [
        new(CaseName: "SimpleSquare", Input: MakeSquare()),
        new(CaseName: "RectangleWithHole", Input: MakeRectangleWithHole()),
        new(CaseName: "LShape", Input: MakeLShape()),
        new(CaseName: "ComplexWithMultipleHoles", Input: MakeComplexWithHoles()),
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

private static Profile2D MakeRectangleWithHole() { /* outer rect + inner rect hole */ }
private static Profile2D MakeLShape() { /* L-shaped polygon */ }
private static Profile2D MakeComplexWithHoles() { /* outer with multiple holes */ }
```

**Assertions:**
- `Mesh.IsValid`
- All faces are triangles (`f.IsTriangle`)
- Vertex count > 0, face count > 0
- `|Mesh.GetArea()|` ≈ profile `AreaMassProperties` area (within 1e-4)
- Degenerate/empty profiles return empty Mesh (zero vertices, zero faces)

## Error Handling

- Self-intersecting profiles → `ArgumentException`
- Degenerate holes (zero area) → silently skipped
- Degenerate/empty profiles → empty Mesh

## Dependencies

None beyond existing `RhinoCommon` reference in AIQ.Urban.Core. No BHoM, no TriangleNet.
