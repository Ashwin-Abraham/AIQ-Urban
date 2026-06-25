# Triangle-to-Quad Converter — Design Spec

## Purpose

Add a `TriangleToQuadConverter.Convert(Mesh)` static method (and public `Mesh.ToQuadMesh()` extension) that takes a triangulated planar 2D mesh and greedily merges adjacent triangle pairs into quadrilateral faces using a diagonal-ratio quality metric. Unmergeable triangles remain as triangles in the output.

## Scope

- Input: `Rhino.Geometry.Mesh` — triangulated, planar at z=0, no self-intersections
- Output: `Rhino.Geometry.Mesh` — mix of quad and triangle faces, same vertices
- Preserves: all input vertices, mesh boundary, total area
- Improves: face count (reduces face count by merging triangles into quads)
- Dependencies: none beyond existing `RhinoCommon` reference

## File Layout

```
src/AIQ.Urban.Core/GeometryOperations/TriangleToQuadConverter.cs   // internal static class
src/AIQ.Urban.Core/Extensions/MeshExtensions.cs                     // public ToQuadMesh() extension
```

### Converter API

```csharp
namespace AIQ.Urban.Core.GeometryOperations;

internal static class TriangleToQuadConverter
{
    public static Mesh Convert(Mesh mesh);
}
```

### Extension API

```csharp
namespace AIQ.Urban.Core.Extensions;

public static class MeshExtensions
{
    public static Mesh ToQuadMesh(this Mesh mesh);
}
```

### Input Validation

| Condition | Behavior |
|---|---|
| `mesh` is `null` | `ArgumentNullException.ThrowIfNull(mesh)` |
| Empty mesh (0 faces) | Return empty `new Mesh()` |
| Non-triangular faces | `ArgumentException("All faces must be triangles")` |
| Vertices not planar XY (z != 0 within tolerance) | `ArgumentException("Mesh must be planar at z=0")` |
| Non-manifold edge (3+ faces share an edge) | Skip that edge — do not merge any pair on it |
| 0–2 triangles in mesh | Return a copy of the input mesh unchanged |

## Algorithm

### Step 1 — Validate Input

Check mesh is non-null, has only triangle faces, and all vertices have z == 0 (within tolerance). Return empty mesh for empty input. For 0–2 faces, return a copy of the input.

### Step 2 — Build Edge-to-Triangle Map

Each triangle face has three edges. Store each edge as a sorted `(int Min, int Max)` value-tuple key mapping to a list of triangle indices that include that edge.

```
edgeToTris = Dictionary<EdgeKey, List<int>>

For each face i with vertices (v0, v1, v2):
    edgeToTris[(min(v0,v1), max(v0,v1))].Add(i)
    edgeToTris[(min(v1,v2), max(v1,v2))].Add(i)
    edgeToTris[(min(v2,v0), max(v2,v0))].Add(i)
```

Edges with exactly two triangles are merge candidates. Edges with one triangle are boundary edges. Edges with three or more are non-manifold and excluded from merging.

### Step 3 — Build Neighbor Lists

For each triangle, collect its neighbor triangle indices — triangles that share an edge and could potentially be merged.

```
neighbors = List<int>[faceCount]

For each edge in edgeToTris where value.Count == 2:
    let t0 = value[0], t1 = value[1]
    neighbors[t0].Add(t1)
    neighbors[t1].Add(t0)
```

### Step 4 — Greedy Merge Loop

```
bool[] merged = new bool[faceCount]
List<(int, int, int, int)> quads = []   // vertex indices
List<(int, int, int)> leftoverTris = []

for i = 0 to faceCount - 1:
    if merged[i]: continue
    
    bestScore = double.MaxValue
    bestNeighbor = -1
    
    for each neighbor j of triangle i:
        if merged[j]: continue
        score = ComputeDiagonalRatio(i, j, mesh)
        if score < bestScore:
            bestScore = score
            bestNeighbor = j
    
    if bestNeighbor >= 0:
        quad = MergeTriangles(i, bestNeighbor, mesh)
        quads.Add(quad)
        merged[i] = true
        merged[bestNeighbor] = true
    else:
        leftoverTris.Add(GetTriangleVertices(i, mesh))
```

### Step 5 — Build Output Mesh

```
Mesh result = new Mesh()
Copy all vertices from input mesh to result
Add all quad faces (v0, v1, v2, v3)
Add all remaining triangle faces (v0, v1, v2, v2)
result.Normals.ComputeNormals()
result.Compact()
return result
```

## Scoring — Diagonal Ratio

For two triangles sharing edge `(s0, s1)` with unshared vertices `uA` (triangle A) and `uB` (triangle B):

```
ComputeDiagonalRatio(triA, triB, mesh):
    (s0, s1) = shared edge vertices
    uA = vertex in triA that is NOT s0 or s1
    uB = vertex in triB that is NOT s0 or s1
    
    d1 = distance(mesh.Vertices[s0], mesh.Vertices[s1])
    d2 = distance(mesh.Vertices[uA], mesh.Vertices[uB])
    
    if min(d1, d2) < tolerance:
        return double.MaxValue  // degenerate, reject
    
    return max(d1, d2) / min(d1, d2)
```

Lower score = better. Score of 1.0 means a perfect square/rhombus. Only positive scores accepted.

## Merging Two Triangles Into a Quad

Given triangle A (va0, va1, va2) and triangle B (vb0, vb1, vb2) sharing edge (s0, s1):

1. Identify the two shared vertices `s0, s1` (present in both triangles)
2. Identify the unshared vertex `uA` (in A only) and `uB` (in B only)
3. Produce quad face with vertices `(uA, s0, uB, s1)` — this is a valid cyclic order

```
MergeTriangles(triA, triB, mesh):
    find s0, s1, uA, uB
    return (uA, s0, uB, s1)
```

The quad vertices are ordered so all four boundary edges exist in the original mesh (either as triangle edges or as the former shared edge's endpoints connecting to opposite unshared vertices). No new vertex positions are created.

## Edge Cases Handled

| Case | Behavior |
|---|---|
| 0–2 triangles | Return copy of input |
| Non-manifold edge (3+ tris) | Skip — no merge on that edge |
| Non-planar mesh | Throw `ArgumentException` |
| Existing quad faces in input | Throw `ArgumentException` |
| Concave resulting quad | Compute signed area of quad; if non-positive, skip that pair |
| Degenerate (zero-area) triangle | Skip merging, keep as-is in output |
| Triangle with no mergeable neighbor | Keep as triangle in output |

## Testing

### Test Framework

NUnit 3.14 with `[RhinoTestFixture]`, same conventions as existing geometry operation tests.

### Test File

`tests/AIQ.Urban.Core.Tests/GeometryOperations/TriangleToQuadConverterTests.cs`

### Test Cases

| Case | Input | Expected |
|---|---|---|
| `TwoTrianglesSquare` | 2 triangles forming a square | 1 quad |
| `SixTrianglesGrid` | 2×3 triangulated grid | 3 quads |
| `FanTriangles` | All triangles share a common vertex | No merges, all remain triangles |
| `LShape` | Triangulated L-shaped polygon | Some quads, some leftover triangles |
| `UShape` | Triangulated U-shaped polygon | Some quads, some leftover triangles |
| `EmptyMesh` | Empty mesh | Empty mesh |
| `SingleTriangle` | 1 triangle | Same single triangle |
| `NonTriangularFaces` | Mesh with a quad face | `ArgumentException` |
| `NonPlanarMesh` | Mesh not at z=0 | `ArgumentException` |
| `NullMesh` | null | `ArgumentNullException` |

### Assertions

- `mesh.IsValid` is `true`
- `mesh.Faces.Count` equals expected face count
- Total mesh area matches input area (within `1e-4`)
- All vertices preserved (same count, same positions)
- For `TwoTrianglesSquare`: verify single quad face with correct 4 vertices
