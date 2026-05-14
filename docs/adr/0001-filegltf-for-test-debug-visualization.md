# ADR 1: FileGltf for test debug visualization

## Status
Accepted

## Context
`AIQ.Urban.Core` uses RhinoCommon types (Curve, Brep, Point3d) for its geometry kernel. Its test project (`AIQ.Urban.Core.Tests`) runs under headless Rhino via `Rhino.Testing` / `[RhinoTestFixture]`.

The repo already has a mature debug visualization pipeline in `ArchSitePlan.DebugModel` that converts BHoM geometry → Hypar.Elements → GLB via `Elements.Serialization.glTF.GltfExtensions.ToGlTF()`. However, this pipeline flows through BHoM types — it cannot directly consume RhinoCommon types.

The options were:
- **A) Build a Rhino→Hypar bridge** and reuse `ArchSitePlan.DebugModel`. This would require adding BHoM + Hypar.Elements (v2.4.0-alpha.9) as transitive dependencies to the test project.
- **B) Use `Rhino.FileIO.FileGltf`** (Rhino 8.3+, already in RhinoCommon). Create a headless `RhinoDoc`, add geometry objects on colored layers, export to GLB. No new dependencies.

## Decision
Use **Option B: `Rhino.FileIO.FileGltf`** for all debug visualization in `AIQ.Urban.Core.Tests`.

## Consequences
- Zero new NuGet dependencies — `RhinoCommon` is already referenced.
- Visualization code is a single `#if DEBUG`-guarded file (`TestVisualizers.cs`) with extension methods on `UrbanModel`.
- Geometry is layered by element type and Use (per-layer colors survive into the GLB via `FileGltfWriteOptions.ExportLayers`).
- GLB files open in the OS default .glb viewer (Windows 3D Viewer, Edge, etc.).
- The approach only works when Rhino is available (headless or not) — it is inherently Windows-only.
- The existing Hypar-based pipeline in `ArchSitePlan.DebugModel` remains untouched and continues to serve the old BHoM-based tests.
