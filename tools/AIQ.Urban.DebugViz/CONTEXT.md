# AIQ.Urban.DebugViz — Debug Preview System

## Purpose

Debug-only visualization pipeline for Rhino geometry during development and testing. Uses a headless `RhinoDoc`, exports to GLB via `Rhino.FileIO.FileGltf`, and opens in either the OS-default viewer or a Babylon.js sandbox. Decoupled from domain projects via an adapter interface to avoid circular dependencies.

## Language

**PreviewSink**:
Concrete implementation of `IPreviewSink`. Owns a headless `RhinoDoc` and a set of converters. Iterates converters to find one that can handle a given object, then inserts the geometry into the doc. Lives in `tools/AIQ.Urban.DebugViz`.
_Avoid_: Sink, PreviewHelper (legacy name)

**PreviewHub**:
Static service locator (guarded by `#if DEBUG`). Holds an `IPreviewSink?` in `Sink` property. Provides `AddToPreviewDoc(this object)` and `PreviewViaHub(this object, bool useWebView)` extension methods that delegate to the sink. The global `using` in `AIQ.Urban.Core` makes these available on every `object` in DEBUG builds.
_Avoid_: Hub, PreviewHelper

**IPreviewSink**:
Adapter interface in `AIQ.PreviewHelper.Core` that decouples domain projects from the concrete preview implementation. Two methods: `AddToPreviewDoc(object)` and `PreviewDoc(bool useWebViewer)`.
_Avoid_: IPreviewAdapter

**IGeometryPreviewConverter**:
Strategy interface in `AIQ.PreviewHelper.Core`. Two methods: `IsPreviewable(object)` as a type guard, and `AddToPreviewDoc(object, RhinoDoc)` to insert geometry. Each converter handles one domain type.
_Avoid_: IGeometryConverter

**UrbanSceneBuilder**:
Builder that creates a headless `RhinoDoc` from an `UrbanModel`, placing each element type on its own layer hierarchy. Consumed by `UrbanModelVisualizers` extension methods. Delegates rendering to the same `IGeometryPreviewConverter` instances used for individual preview, so visualization is consistent regardless of entry point.

### Converter hierarchy

Converters live in `Converters/` under two tiers:

**Geometry-level converters** convert geometry-wrapper types from `AIQ.Urban.Core`:
- `Curve2DConverter` — extracts `.Geometry` (Rhino `Curve`)
- `Polygon2DConverter` — extracts `.Geometry` (Rhino `PolylineCurve`)
- `CurveElementConverter` — delegates to `Curve2DConverter`
- `BrepElementConverter` — extracts `.Geometry` (Rhino `Brep`)
- `Profile2DConverter` — output outline + hole boundary curves via `Polygon2DConverter`

**Urban-element converters** convert `IUrbanElement` subtypes and reference geometry-level converters via `private readonly` instance fields (direct instantiation at construction time):
- `FloorPlateConverter` — planar Brep mesh, colored by `Use`
- `BuildingConverter` — envelope Brep mesh + per-floorplate meshes; references `BrepElementConverter` + `FloorPlateConverter`
- `TransitConverter` — centerline curve + outline curves, colored by `TransitType`; references `Curve2DConverter` + `Profile2DConverter`
- `TransitSurfacesConverter` — takes `IEnumerable<(Profile2D, TransitType)>`; delegates to `Profile2DConverter` per pair
- `ParcelConverter` — planar Brep mesh, colored by `Use`; references `Polygon2DConverter`
- `ZoneConverter` — boundary curve only, hardcoded `Color.CornflowerBlue`; references `Polygon2DConverter`

### Layer naming

Converters use the same nested layer hierarchy as the Rhino plugin output conventions (see `src/AIQ.Urban.Rhino/CONTEXT.md`), so debug previews match baked output.

## Relationships

- **PreviewAssemblyBootstrap** initializes **PreviewHub.Sink** with a **PreviewSink** — runs automatically when the `AIQ.Urban.DebugViz` assembly loads
- **PreviewHub** delegates to **IPreviewSink.AddToPreviewDoc** / **PreviewDoc**
- **PreviewSink** loops over its `HashSet<IGeometryPreviewConverter>`, finds the matching converter, and calls `AddToPreviewDoc(object, RhinoDoc)`
- **PreviewSink.PreviewDoc** calls **Visualizer.View** to export and display
- **UrbanSceneBuilder** holds instances of all converters and delegates to them when building the full scene

## Known bugs

- **ReportFailedType unconditional call**: `PreviewSink.AddToPreviewDoc` calls `ReportFailedType` unconditionally after the converter loop instead of only when no converter matched — this produces false-positive "No preview converter loaded" trace messages even on successful conversions.

## Example dialogue

> **Dev:** "Why does `PreviewHub` exist at all? Why not just call `PreviewSink.AddToPreviewDoc` directly?"
> **Domain expert:** "Because `AIQ.Urban.Core` can't reference `AIQ.Urban.DebugViz` — that'd be a circular dependency. `IPreviewSink` lives in `AIQ.PreviewHelper.Core` which both projects reference. `PreviewHub` is the static glue that lets domain code call `myObject.AddToPreviewDoc()` without knowing about `PreviewSink` at compile time."

> **Dev:** "What happens if no converter matches my object?"
> **Domain expert:** "`PreviewSink.AddToPreviewDoc` will still call `ReportFailedType`, which writes a trace message. There's no short-circuit after a successful match — every non-matching converter will also fire `ReportFailedType`. If a converter matches, the geometry gets added to the headless RhinoDoc. Otherwise only the trace message appears and nothing is previewed."

## Flagged ambiguities

- **"kope.PreviewHelper.Converters"** was the legacy namespace from before the AIQ rename — all references have been updated to `AIQ.Urban.DebugViz.Converters`
- **"previewStyle"** was a leftover parameter from the old `DEBUGMODEL.Debug(..., Material?)` pattern — the interfaces and extension methods no longer have this parameter
- **"ReportFailedType" bug**: `PreviewSink.AddToPreviewDoc` calls `ReportFailedType` unconditionally after the converter loop instead of only when no converter matched — this produces false-positive "No preview converter loaded" trace messages even on successful conversions
