# AIQ.Urban.RhinoPlugin — Context

## Purpose
Rhino 8 plugin (`AIQ.Urban.RhinoPlugin.rhp`) owning the native Rhino commands and the document event handler that bridges Rhino's interaction model with the domain in `AIQ.Urban.Core`.

## Design Constraints
- **Nullable reference types enabled** — `?` marks values that can legitimately be null; unadorned types are assumed non-null and need not be checked.
- References `AIQ.Urban.RhinoInterface` for pipeline orchestration
- References `RhinoCommon` directly for Rhino UI (doc, views, selection)
- Pipeline supports post-actions via `.WithUrbanModelAction(Action<UrbanModel>)` — registered actions execute after all steps, deletions and baking complete.
- **Commands for creation, events for mutation/deletion**: explicit Rhino commands (`AddTransit_AIQUrban`, `AddBuilding_AIQUrban`, etc.) are used to create urban elements. Document events (`ReplaceRhinoObject`, `DeleteRhinoObject`) handle modification and deletion after the fact. A user drawing geometry on a managed layer does NOT automatically create an urban element — that requires an explicit command.
- **RhinoUrbanEventHandler**: instance class implementing `IDisposable`, owned by `Urban_RhinoPlugin`. Subscribes to RhinoDoc events in its constructor, unsubscribes in `Dispose()`. Lives in `AIQ.Urban.RhinoPlugin` (not `RhinoInterface`) because it manages RhinoDoc interaction lifecycle.
- **Deferred event processing**: events are queued into lists (add/remove/replace), then processed on `RhinoApp.Idle` to coalesce multiple Rhino events from a single user action. An `EventLock` guard suppresses event capture while the pipeline writes geometry back to Rhino, preventing re-entrant feedback loops.
- **Events subscribed**: `ReplaceRhinoObject` (modification), `DeleteRhinoObject` (deletion), `BeginOpenDocument` / `EndOpenDocument` (file load lifecycle), `RhinoApp.Idle` (processing trigger). **Not** subscribed: `AddRhinoObject` (objects are only added via explicit commands). `UndeleteRhinoObject` deferred for now.
- **Reverse-lookup dictionary**: `Dictionary<Guid, IUrbanElement>` maintained alongside `UrbanModel`, keyed by Rhino ObjectId. Updated when the pipeline bakes new geometry or deletes elements. Used by the event handler to correlate a modified/deleted Rhino object to its owning `IUrbanElement`.
- **Layer filtering**: the event handler checks each object's layer path. Only objects on layers starting with `AIQ Urban::` are processed. Non-urban geometry is ignored.
- **Reverse-lookup dict lifecycle**: the handler owns the dict and wraps each pipeline run. After `pipeline.Run()` returns, the handler iterates the added/deleted GUIDs to update the dict. On `ReplaceRhinoObject`, the handler swaps old ObjectId → new ObjectId in the dict.
- **Error on miss**: reverse-lookup miss is silently skipped. The dict is correctly maintained during a session; after file open there's nothing to do (accepted gap).
- **File open gap**: on `EndOpenDocument`, UrbanModel is empty (in-memory singleton reset). The reverse-lookup dict is also empty. No reconciliation pass yet — user must re-run commands. Accepted gap for v1.
- Vue UI via websocket/HTTPS is a future concern

## Event Handler Lifecycle
- Created in `Urban_RhinoPlugin.OnLoad()` → calls `SubscribeModelEvents()` on the singleton handler
- Disposed in `Urban_RhinoPlugin.OnUnload()` → calls `Dispose()` which unsubscribes all events
- `EventLock` guards against event processing during file loads, pipeline output, and bulk operations

## RhinoUrbanModelPipeline
Fluent pipeline in `Pipeline/RhinoUrbanModelPipeline.cs` under the `AIQ.Urban.RhinoInterface` namespace. Step-based, with a single ordered step list:

```
new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithRhinoObjectDeletion(inputCurveIds)      // deletes Rhino doc objects directly
    .WithUrbanElementDeletion(deleteStep)         // removes from UrbanModel, returns IDs for Rhino cleanup
    .WithUrbanElementAddition(addStep)            // adds to UrbanModel, returns geometry IDs for baking
    .Run();
```

- **WithRhinoObjectDeletion**: Registers Rhino ObjectIds to delete from the doc. Independent of UrbanModel state — used for input curves consumed during command execution.
- **WithUrbanElementDeletion**: Appends `IDeleteUrbanElementsStep` to the single step list.
- **WithUrbanElementAddition**: Appends `IAddUrbanElementsStep` to the single step list.
- **Run()**: Iterates the step list in configuration order, calling each `Run(UrbanModel)`. Each step's returned GUIDs are routed to the delete, edit, or add bucket based on which marker interfaces the step implements (a step may implement multiple interfaces, e.g. `RecalculateParcelsStep` implements both `IAdd` and `IDelete` to return old parcel GUIDs for cleanup and new parcel GUIDs for baking). Then batch-deletes Rhino objects and bakes new geometry.

Element type output is automatic — `OutputTransits()`, `OutputBuildings()`, `OutputParcels(addedIds)`, and `OutputZones(addedIds)` are called by the pipeline for any added elements of those types.

## Testing Strategy

The event handler is tested at two levels:

1. **Unit tests** (`AIQ.Urban.Core.Tests`, no Rhino dependency): an extracted `EventQueue` class handles queuing, deduplication, and dispatch routing. Tested with direct method calls — no Rhino doc needed.
2. **Integration tests** (`tests/AIQ.Urban.RhinoPlugin.Tests`, headless Rhino via `[RhinoTestFixture]`): `RhinoUrbanEventHandler` is wired to a real `RhinoDoc`; programmatic add/delete/replace operations verify end-to-end queuing → pipeline dispatch → UrbanModel state.

Core steps (`RemoveBuildingStep`, etc.) follow the existing step-testing pattern in `AIQ.Urban.Core.Tests`.

## RhinoUrbanEventHandler — Dispatch Logic

The event handler dispatches `DeleteRhinoObject` and `ReplaceRhinoObject` events by element type. Buildings are skipped for Replace (deferred to later stage).

The handler checks the modified object's layer to decide if it's authoritative geometry (Centerline for Transit, Boundary curve for Zone). Objects on derived layers (Outline, Surface, Envelope, FloorPlates) are re-baked on pipeline runs and user modifications are overwritten — that is correct behaviour.

### Pipeline Cascade Matrix

Each event triggers a pipeline configuration with primary steps plus cascading recomputation:

| Event | Primary step(s) | Cascade |
|---|---|---|
| Transit Delete | `RemoveTransitStep` → | `TransitSurfaceUnioner` → `RecalculateParcelsStep` → Metrics |
| Transit Replace | `UpdateTransitStep` → | `TransitSurfaceUnioner` → `RecalculateParcelsStep` → Metrics |
| Building Delete | `RemoveBuildingStep` → | Metrics |
| Zone Delete | `RemoveZoneStep` → | Metrics |
| Zone Replace | `UpdateZoneStep` → | Metrics |
| Building Replace | *(deferred)* | |
| Parcel (any) | *(ignored — parcels are derived)* | |

### New Core Steps Needed

| Step | Interface | Role |
|---|---|---|
| `RemoveBuildingStep(List<Guid> ids)` | `IDeleteUrbanElementsStep` | Removes Buildings from UrbanModel by geometry ID match |
| `RemoveZoneStep(List<Guid> ids)` | `IDeleteUrbanElementsStep` | Removes Zones from UrbanModel |
| `UpdateTransitStep(Guid oldCenterlineId, Curve newCenterline)` | `IEditUrbanElementsStep` | Updates a Transit's centerline in-place, keeps width/type |
| `UpdateZoneStep(Guid oldBoundaryId, Curve newBoundary)` | `IEditUrbanElementsStep` | Updates a Zone's boundary polygon in-place |

These pipeline configurations are wrapped as reusable methods (not necessarily user-facing commands) so both the event handler and any future explicit command can call them.

## Command Surface

### `AddTransit_AIQUrban`
- User picks curves as centerlines (planar, Z variation validated and rejected)
- Prompts: `GetDouble` for width (default 7.0), `GetInteger` for TransitType (0=Road…)
- Pipeline: `.WithRhinoObjectDeletion(centerlineIds).WithUrbanElementDeletion(removeExisting).WithUrbanElementAddition(addTransitStep).WithUrbanElementAddition(transitUnioner).WithUrbanElementAddition(recalculateParcelsStep).Run()`
- `TransitSurfaceUnioner` is registered as an `IAddUrbanElementsStep` — unions all transit outlines per type into `UrbanModel.TransitSurfaces`
- `RecalculateParcelsStep` runs after `TransitSurfaceUnioner` — reads `UrbanModel.TransitSurfaces` holes to create parcels
- Only the `Outline` and `Surface` are baked; input curves are deleted by `WithRhinoObjectDeletion`
- Duplicate detection via `RemoveTransitStep`: if same centerline ObjectId was previously used, old Transit is removed from UrbanModel and baked outline deleted from Rhino doc
- **Known gap**: When transit is deleted/modified, previously baked unioned `Surface` geometry remains as orphans in the Rhino doc — no cleanup yet. Parcel geometry for old parcels is correctly cleaned up since `RecalculateParcelsStep` returns old parcel GUIDs as `IDeleteUrbanElementsStep` output.

### `AddBuilding_AIQUrban`
- Collects: footprint curve, floor height, floor count, Use dropdown
- Validates footprint is planar
- Pipeline: `.WithRhinoObjectDeletion([footprintId]).WithUrbanElementAddition(addBuildingStep).Run()`
- Input curve is deleted by `WithRhinoObjectDeletion` (not preserved in the doc)
- No duplicate detection for now (input curve is destroyed, same ObjectId can never match again)
- Only footprint+height+count+use variant; Brep/parcel variants deferred

### `AddZone_AIQUrban`
- User picks a closed planar curve as the zone boundary, then enters a zone name via text prompt
- Validates: curve is closed and planar, name does not contain `::`, name is not already taken by an existing zone
- Pipeline: `.WithRhinoObjectDeletion([boundaryId]).WithUrbanElementAddition(addZoneStep).Run()`
- Input curve is deleted by `WithRhinoObjectDeletion`
- Duplicate detection: if a zone with the same name exists, warns user and aborts

### `ChangeBuildingUse_AIQUrban`
- Single command for modifying the use of built elements (buildings and/or individual floor plates)
- Accepts selection of: building envelope Breps (→ building-wide change) + floor plate outline curves (→ per-plate change)
- User picks any mix of Breps and curves in a single `GetObject` pass, then selects a new Use from the DefaultUses dropdown
- The step receives a flat `List<Guid>` and inspects `UrbanModel.Buildings` to resolve each GUID:
  - Envelope GUID → find owning Building → set all its floor plates (building-wide)
  - Floor plate outline GUID → find owning Building → set that specific plate (per-plate)
- Building-wins priority: envelopes processed first; if a Building was already touched, its individual plates are skipped in the per-plate pass
- Pipeline: `.WithUrbanElementStep(changeStep).WithMetricsCalculation(...).Run()`
- Re-bakes the entire parent Building whenever any of its floor plates change (envelope + all plates) — no separate `FloorPlateOutputter`
- `Building.Use` property removed from the domain model; each `FloorPlate` owns its own Use

### `ChangeParcelUse_AIQUrban`
- Changes the use of one or more parcels selected in the viewport
- User picks parcel outlines (Polygon2D curves baked to `AIQ Urban::Parcel::{use.Name}` layers), then selects new Use from DefaultUses dropdown
- Step mutates `Parcel.Use` in-place via `SetUse()`
- Pipeline: `.WithUrbanElementStep(changeStep).WithMetricsCalculation(...).Run()`
- See Note on Parcel Use Preservation: explicit use changes may be lost if transits are later modified, because `RecalculateParcelsStep` preserves use by nearest-centroid matching

### `ResetUrban_AIQUrban`
- Destructive command: deletes all baked Rhino geometry and clears the urban model
- Confirmation prompt: user must type "yes" to proceed
- Pipeline: `.WithUrbanElementStep(new ResetUrbanElementsStep()).Run()`
- `ResetUrbanElementsStep` collects all geometry GUIDs from all element types (Transits, Buildings, Parcels, Zones) and `TransitSurfaces`, then calls `UrbanModel.Reset()`
- No metrics calculation after reset
- Layers remain in the Rhino doc (only objects are deleted)

### Future Commands (non-exhaustive, deferred)
- `AddBuildingFromBrep(Brep Envelope, double FloorHeight)`
- `AddBuildingOnParcel(Brep Envelope, double FloorHeight)`
- `AddBuildingOnParcelEdge(Brep Envelope, double FloorHeight)`

### ProjectMetricsJsonSerializer
Serializer in `Pipeline/ModelActions/ProjectMetricsJsonSerializer.cs`. Reads `UrbanModel.ProjectMetrics` and writes it as indented JSON to `%USERPROFILE%\Documents\AIQ Urban\{RhinoDocName}-project-metrics.json`. Invoked as a `WithUrbanModelAction` step after the pipeline computes metrics. Uses `System.Text.Json` (no additional dependencies).

## Relationship to Core
Commands collect user input, then delegate to `RhinoUrbanModelPipeline` which calls into Core's domain logic. Core owns all model state and computation. The RhinoPlugin is a thin presenter/controller layer on top.

## Rhino Output Conventions

### Profile2D Bake Modes
`asBrep` means bake the planar `Profile2D` Brep surface to Rhino. When false, bake its outer outline and hole boundaries as curves. Avoid `solid` for this option because a `Profile2D` is a planar surface, not a closed volumetric solid.

### Layer Structure
All baked geometry follows a consistent nested layer hierarchy:
```
AIQ Urban
  ├── Transit
  │   ├── Road
  │   │   ├── Centerline
  │   │   ├── Outline
  │   │   └── Surface        (unioned per-type, not per-width)
  │   └── ...
  ├── Building
  │   ├── Envelope
  │   └── FloorPlates
  │       ├── Residential   (one per distinct Use)
  │       └── ...
  ├── Zone
  │   ├── Downtown          (one per distinct zone name)
  │   └── Park
  ├── Parcel
  ├── Vegetation
  └── Topography
```
Each element type bakes into its corresponding child layer under `AIQ Urban`.
Layers are created on first use if they don't exist.

Floor plate outlines group by Use across all buildings — all "Residential" plates from all buildings share one sublayer.
