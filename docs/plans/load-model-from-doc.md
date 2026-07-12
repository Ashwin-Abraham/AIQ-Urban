# Plan: Load Urban Model from Rhino Doc on StartUI

## Goal

When `StartUI_AIQUrban` is run, scan the existing baked geometry in the Rhino doc and hydrate `UrbanModel` from it. The Rhino doc is treated as the source of truth — UrbanModel is nuked and rebuilt from scratch.

## Loading Order

1. **Reset UrbanModel** — `UrbanModel.Instance.Reset()` clears all collections
2. **Load Uses** — ensure all use definitions are in `UrbanModel.Uses` (from `DefaultUses`)
3. **Load Transits** — scan centerline layers, parse type + width from layer name, create `Transit` objects
4. **Capture old doc parcel centroids** — scan existing parcel geometry for `(centroid, use)` pairs before recalculation
5. **Union transit surfaces** — run `TransitSurfaceUnioner` to recompute `UrbanModel.TransitSurfaces`
6. **Recalculate parcels** — run modified `RecalculateParcelsStep` with doc-sourced centroid/use pairs
7. **Load buildings** — scan envelope Breps + floor plate curves, match via 3D centroid containment, create `Building` objects
8. **Load zones** — scan boundary curves, create `Zone` objects
9. **Metrics** — run `ProjectMetricsCalculator`
10. **Rebuild event handler lookup**
11. **Start UI** (existing HTTP/WS/Eto logic)

## Design Decisions

### Transit Loading
- Scan `AIQ Urban::Transit::{Type}::Centerline::{width}` layers for centerline curves
- Parse `TransitType` and `Width` from the layer path string
- Create `Transit(Curve2D, width, transitType)` objects
- Outlines and surfaces are **not** scanned — they are recomputed by `TransitSurfaceUnioner` after loading

### Building Loading (New Constructor)
- Scan `AIQ Urban::Building::Envelope` for Brep objects
- Scan `AIQ Urban::Building::FloorPlates::*` for curve objects
- Parse `Use` from floor plate layer name (`FloorPlates::{UseName}`)
- Associate floor plates to envelopes: compute each floor plate's 3D centroid, find the first envelope Brep that contains it via `Brep.Containment()`
- Create `Building(Brep envelope, IReadOnlyList<FloorPlate> existingPlates)` — new constructor that preserves existing floor plate curves
- `TypicalFloorHeight` computed as the **mode** of non-zero Z-differences between consecutive floor plates (tolerance ~1e-6). Fallback to `3.0` if no valid differences.
- `Footprint` = lowest floor plate's outline
- `FloorCount` = `existingPlates.Count`
- Orphaned floor plates (no containing envelope) are silently skipped

### Parcel Use Preservation
- Before `RecalculateParcelsStep` runs, scan existing parcel Brep geometry on `AIQ Urban::Parcel::*` layers
- Compute centroid + parse use from layer name → `List<(Point3d Centroid, Use Use)>`
- Pass these pairs to the modified `RecalculateParcelsStep` so it uses them for nearest-centroid matching instead of reading from (empty) `UrbanModel.Parcels`

### Zone Loading
- Scan `AIQ Urban::Zone::{name}` layers for closed curves
- Parse zone name from layer path
- Create `Zone(Polygon2D, name)` objects

### Error Handling
- Silent skip for orphaned geometry (floor plates with no envelope, etc.)
- No abort on missing geometry — load what's available

## Implementation Steps

### Step 1: Modify `RecalculateParcelsStep` to accept external centroids

**File:** `src/AIQ.Urban.Core/ModelActions/RecalculateParcelsStep.cs`

Add an optional parameter to `Run()`:
```csharp
public EditStepResult Run(UrbanModel model, List<(Point3d Centroid, Use Use)>? externalCentroids = null)
```

When `externalCentroids` is provided and non-empty, use those for `ResolveUse` instead of computing centroids from `model.Parcels`. Existing callers pass `null` (default) and get current behavior.

### Step 2: Add `Building` constructor for existing floor plates

**File:** `src/AIQ.Urban.Core/Models/UrbanElements/Building.cs`

New constructor:
```csharp
public Building(Brep envelope, IReadOnlyList<FloorPlate> existingPlates)
```

- Stores envelope as `BrepElement`
- Stores plates as `FloorPlates`
- Computes `Footprint` from lowest plate's outline
- Computes `TypicalFloorHeight` as mode of non-zero Z-differences between consecutive plates (tolerance 1e-6), fallback `3.0`
- `FloorCount` = `existingPlates.Count`
- `NodePoint` = `GetRepresentativeCenter()` (centroid of lowest plate)

### Step 3: Add `WithDocModelLoad` to `PipelineConfigurations`

**File:** `src/AIQ.Urban.RhinoInterface/Pipeline/PipelineConfigurations.cs`

New extension method:
```csharp
public static RhinoUrbanModelPipeline WithDocModelLoad(
    this RhinoUrbanModelPipeline pipeline,
    RhinoDoc doc,
    RhinoUrbanEventHandler eventHandler)
```

This method:
1. Resets `UrbanModel.Instance`
2. Ensures default uses are loaded
3. Scans doc for centerline curves on `AIQ Urban::Transit::*::Centerline::*` layers
4. Parses transit type and width from layer paths
5. Creates `Transit` objects and adds to model
6. Scans doc for existing parcel geometry on `AIQ Urban::Parcel::*` layers, captures `(centroid, use)` pairs
7. Configures transit surface unioner + parcel recalculation steps (with doc-sourced centroids)
8. Scans doc for envelope Breps on `AIQ Urban::Building::Envelope` layer
9. Scans doc for floor plate curves on `AIQ Urban::Building::FloorPlates::*` layers
10. Matches floor plates to envelopes via 3D centroid containment
11. Creates `Building` objects with the new constructor
12. Scans doc for zone boundary curves on `AIQ Urban::Zone::*` layers
13. Creates `Zone` objects
14. Configures metrics calculation
15. Configures event handler

Returns the configured pipeline (caller calls `.Run()`).

### Step 4: Update `StartUI_AIQUrban` to load model

**File:** `src/AIQ.Urban.Rhino/Commands/StartUI_AIQUrban.cs`

Before starting UI servers, build and run the loading pipeline:
```csharp
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithDocModelLoad(doc, Urban_RhinoPlugin.Instance.EventHandler);
pipeline.Run();
```

Then proceed with existing UI startup logic.

### Step 5: Add layer name parsing utilities

**File:** `src/AIQ.Urban.RhinoInterface/Utils/LayerConstants.cs` or new `LayerNameParser.cs`

- `ParseTransitType(string layerPath)` → `TransitType?`
- `ParseTransitWidth(string layerPath)` → `double?`
- `ParseUseName(string layerPath)` → `string?`
- `ParseZoneName(string layerPath)` → `string?`

Inverse of the existing `LayerConstants.TransitCenterline()`, `LayerConstants.TransitOutline()`, etc.

### Step 6: Add Rhino doc scanning utilities

**File:** new `src/AIQ.Urban.RhinoInterface/Utils/RhinoDocScanner.cs` or within `PipelineConfigurations`

Methods:
- `FindCurvesOnLayer(RhinoDoc doc, string layerPattern)` → `List<(Curve Curve, Guid ObjectId, string LayerPath)>`
- `FindBrepsOnLayer(RhinoDoc doc, string layerPattern)` → `List<(Brep Brep, Guid ObjectId, string LayerPath)>`

Uses `doc.Objects` iteration with layer path matching.

### Step 7: Tests

- **`RecalculateParcelsStep` tests**: verify that passing external centroids uses those for use resolution, and that `null` preserves existing behavior
- **`Building` constructor tests**: verify TypicalFloorHeight computation (mode of Z-differences), footprint from lowest plate, floor count
- **Layer parsing tests**: verify round-trip — `LayerConstants.TransitCenterline(type, width)` → parse back → same type and width
- **Integration test** (headless Rhino): build a Rhino doc with baked geometry, run `WithDocModelLoad`, verify UrbanModel state matches expected

## Open Questions

None — all design decisions resolved during grilling.
