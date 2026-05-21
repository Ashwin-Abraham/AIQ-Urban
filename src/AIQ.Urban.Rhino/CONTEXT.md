# AIQ.Urban.RhinoInterface — Context

## Purpose
Rhino 8 interface providing native commands for urban site modelling. Acts as the bridge between Rhino's document/interaction model and the domain in `AIQ.Urban.Core`.

## Design Constraints
- **Nullable reference types enabled** — `?` marks values that can legitimately be null; unadorned types are assumed non-null and need not be checked.
- References `AIQ.Urban.Core` for all domain logic and geometry kernel
- References `RhinoCommon` directly for Rhino UI (doc, views, selection)
- Pipeline supports post-actions via `.WithUrbanModelAction(Action<UrbanModel>)` — registered actions execute after all steps, deletions and baking complete. Used for model-only side effects such as metrics calculation and serialization.
- All initial interaction is via native Rhino commands (no UI yet)
- Vue UI via websocket/HTTPS is a future concern
- Multi-step sync between Rhino doc and UrbanModel flows through `RhinoUrbanModelPipeline` in `Pipeline/`

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
- `ChangeUse(Group | Parcel2D Parcel, Use Use)`

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
