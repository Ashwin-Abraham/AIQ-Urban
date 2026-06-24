# Delaunay Refinement — Design Spec

## Purpose

Add a `DelaunayRefiner.Refine(Mesh)` static method that takes any triangulated planar XY mesh and improves it by enforcing the Delaunay condition via iterative edge flipping, as described in Step 5 of Nail Sharipov's sweep-line triangulation article (https://dev.to/nail_sharipov_5d810d8cf71/monotone-triangulation-practical-advice-1k4j).

This is a post-processing step on an existing triangulation — not a from-scratch Delaunay builder. Boundary edges are preserved; only interior edges are flipped.

## Scope

- Input: `Rhino.Geometry.Mesh` — triangulated, planar at z=0, XY-aligned
- Output: `Rhino.Geometry.Mesh` — same vertices, same area, Delaunay-quality topology
- Preserves: all input vertices, mesh boundary, total area
- Improves: triangle quality (minimizes skinny triangles)
- Dependencies: none beyond existing `RhinoCommon` reference
- **No new NuGet packages, no BHoM, no TriangleNet**

## File Layout

```
src/AIQ.Urban.Core/GeometryOperations/DelaunayRefiner.cs
```

Single file, `internal static class DelaunayRefiner` in namespace `AIQ.Urban.Core.GeometryOperations`.

### API

```csharp
namespace AIQ.Urban.Core.GeometryOperations;

internal static class DelaunayRefiner
{
    public static Mesh Refine(Mesh mesh, double tolerance = 1e-5);
}
```

### Input Validation

| Condition | Behavior |
|---|---|
| `mesh` is `null` | `ArgumentNullException.ThrowIfNull(mesh)` |
| Empty mesh (0 faces) | Return empty `new Mesh()` |
| Non-triangular faces | `ArgumentException("All faces must be triangles")` |
| Vertices not planar XY (z != 0) | `ArgumentException("Mesh must be planar at z=0")` |
| Degenerate triangles (zero area) | Skipped during adjacency build |
| Non-convergence after `count * count` passes | Break loop, return best-effort Mesh |

## Algorithm

### Step 1 — Validate Input

Check mesh is non-null, has only triangle faces, and all vertices have z == 0 (within `tolerance`). Return empty mesh for empty input.

### Step 2 — Build Triangle Adjacency

For each mesh face, compute its three edges as sorted `(minVertexIndex, maxVertexIndex)` tuples. Build a dictionary mapping each edge to the list of face indices that share it.

- Edges with exactly one face → boundary edge (excluded from flipping)
- Edges with exactly two faces → interior edge (candidate for flipping)
- Edges with >2 faces → non-manifold topology (`ArgumentException`)

Store adjacency as two `int` arrays per triangle: `neighborA`, `neighborB`, `neighborC` where each entry is the face index of the adjacent triangle across that edge, or `-1` for boundary edges. Slot `i` holds the neighbor opposite vertex `i` of the triangle.

### Step 3 — Edge-Flipping Loop

```
for pass = 0; pass < maxPasses; pass++:
    flippedAny = false
    for each interior edge (shared by triangles T1, T2):
        let quad vertices be A, B, C, D in CCW order around the quadrilateral
        if quadrilateral is not convex → skip (flip would produce invalid topology)
        if InCircle(A, B, C, D) or InCircle(D, B, C, A) → flip needed:
            // Flip: replace shared edge AC with edge BD
            // T1(A,B,C) + T2(A,C,D) → T1'(A,B,D) + T2'(B,C,D)
            UpdateMesh(T1, T2, A, B, C, D)
            UpdateAdjacency(T1', T2')
            flippedAny = true
            processedThisPass++
    if !flippedAny → break (converged)
```

Max passes = `faceCount * faceCount` (safety limit, should never trigger in practice).

### Step 4 — Rebuild Mesh

After all passes complete, build a new `Mesh` from the updated topology:
- Copy all vertices from input (unchanged)
- Add all faces from updated face list
- Return the new mesh

## InCircle Predicate

The Delaunay condition test uses the standard 4×4 determinant (orient2D基础上的InCircle)：

Given triangle `(A, B, C)` and point `D`, `D` is inside the circumcircle of `△ABC` when:

```
InCircle(A, B, C, D) = det > 0
```

where the determinant is:

```
| Ax  Ay  Ax²+Ay²  1 |
| Bx  By  Bx²+By²  1 |
| Cx  Cy  Cx²+Cy²  1 |
| Dx  Dy  Dx²+Dy²  1 |
```

This is the standard robust geometric predicate for the Delaunay condition. For a detailed explanation and worked example, see: https://www.cs.cmu.edu/~quake/robust.html

Implementation notes:
- Compute as 3×3 minors using the Laplace expansion along the last column (all `±1` entries), which yields the standard 4-term cross-product form.
- Use `double` arithmetic. At our coordinate scale (0–1000) and tolerance `1e-5`, this is numerically stable.
- The predicate returns `true` when `det > tolerance` (strictly inside circumcircle).

## Edge Flip Detail

Given two triangles sharing an interior edge:

```
Before:                  After:
   B                       B
  / \                     /|\
 /   \                   / | \
A-----C       →         A  |  C
 \   /                   \ | /
  \ /                     \|/
   D                       D
```

- Shared edge `AC` is removed
- New edge `BD` is created
- Old triangles: `(A, B, C)` and `(A, C, D)`
- New triangles: `(A, B, D)` and `(B, C, D)` (both CCW preserving)
- Adjacency for the four affected triangles (2 old + 2 new) is rebuilt
- The `neighbors` array is updated for both new triangles and any triangle adjacent to them

## Testing

### Test Framework

NUnit 3.14 with `[RhinoTestFixture]`, same conventions as `SweepLineTriangulatorTests`.

### Test File

`tests/AIQ.Urban.Core.Tests/GeometryOperations/DelaunayRefinerTests.cs`

### Test Pattern

Parametrized `[TestCase("CaseName")]` + private record + static `_testData`:

```csharp
private record DelaunayTestData(
    string CaseName,
    Mesh Input,
    int ExpectedTriangleCount,
    double ExpectedAverageArea
);
```

### Test Cases

| Case | Input | What it tests |
|---|---|---|
| `SimpleSquare` | 4-vertex ear-clipped square (2 tris) | Already Delaunay, edge-flip is no-op |
| `LShape` | Ear-clipped L-shaped polygon | Concave boundary preserved |
| `Jagged` | Ear-clipped jagged polygon | Many interior edges to test flipping |
| `UShape` | Ear-clipped U-shaped polygon | Concave boundary preserved |
| `NonDelaunayQuad` | A purposely bad triangulation of a convex quad (split wrong way) | Verifies the flipper corrects it |
| `Degenerate` | Empty mesh | Returns empty mesh |

### Assertions

- `mesh.IsValid` is `true`
- `mesh.Faces.Count` equals `ExpectedTriangleCount`
- `mesh.Faces.All(f => f.IsTriangle)` is `true`
- Mesh area matches input area (within `1e-4`)
- Average triangle area matches expected (within `1e-2`)
- For `NonDelaunayQuad`: verify the diagonal was flipped (check edge exists between the correct vertices)

### NonDelaunayQuad Test Data

Create a convex, non-cyclic quadrilateral `(0,0), (10,0), (9,10), (1,9)` but triangulate it with the "wrong" diagonal (the one that violates Delaunay). Verify that after refinement, at least one edge flip occurred (compare face topology before and after).

## Error Handling

- Self-intersecting or non-manifold input → `ArgumentException`
- Boundary edges are never flipped (verified by adjacency count == 1)
- Non-convergence guard: `maxPasses = faceCount * faceCount` prevents infinite loops
- Tolerance parameter threaded to all geometric tests (planarity, InCircle, degenerate triangle detection)
