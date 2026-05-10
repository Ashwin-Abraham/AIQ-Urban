# AIQ.Urban.RhinoPlugin — Context

## Purpose
Rhino 8 plugin providing native commands for urban site modelling. Acts as the bridge between Rhino's document/interaction model and the domain in `AIQ.Urban.Core`.

## Design Constraints
- References `AIQ.Urban.Core` for all domain logic and geometry kernel
- References `RhinoCommon` directly for Rhino UI (doc, views, selection)
- All initial interaction is via native Rhino commands (no UI yet)
- Vue UI via websocket/HTTPS is a future concern
- Multi-step sync between Rhino doc and UrbanModel flows through `RhinoUrbanModelPipeline` in `Pipeline/`

## RhinoUrbanModelPipeline
Fluent pipeline in `Pipeline/RhinoUrbanModelPipeline.cs`. Step-based, with three operation categories:

```
new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithRhinoObjectDeletion(inputCurveIds)      // deletes Rhino doc objects directly
    .WithUrbanElementDeletion(deleteStep)         // removes from UrbanModel, returns IDs for Rhino cleanup
    .WithUrbanElementAddition(addStep)            // adds to UrbanModel, returns geometry IDs for baking
    .Run();
```

- **WithRhinoObjectDeletion**: Registers Rhino ObjectIds to delete from the doc. Independent of UrbanModel state — used for input curves consumed during command execution.
- **WithUrbanElementDeletion**: Runs `IDeleteUrbanElementsStep` which removes elements from UrbanModel and returns their baked geometry IDs for Rhino doc cleanup.
- **WithUrbanElementAddition**: Runs `IAddUrbanElementsStep` which creates domain elements, adds them to UrbanModel, and returns their geometry IDs for baking.
- **Run()**: Executes in order: Delete → (model edits) → Add, then batch-deletes Rhino objects and bakes new geometry.

Element type output is automatic — `OutputTransits()` and `OutputBuildings()` are called by the pipeline for any added elements of those types.

## Command Surface

### `AddTransit_AIQUrban`
- User picks curves as centerlines (planar, Z variation validated and rejected)
- Prompts: `GetDouble` for width (default 7.0), `GetInteger` for TransitType (0=Road…)
- Pipeline: `.WithRhinoObjectDeletion(centerlineIds).WithUrbanElementDeletion(removeExisting).WithUrbanElementAddition(addTransitStep).Run()`
- Only the `Outline` is baked; input curves are deleted by `WithRhinoObjectDeletion`
- Duplicate detection via `RemoveTransitStep`: if same centerline ObjectId was previously used, old Transit is removed from UrbanModel and baked outline deleted from Rhino doc

### `AddBuilding_AIQUrban`
- Collects: footprint curve, floor height, floor count, Use dropdown
- Validates footprint is planar
- Pipeline: `.WithRhinoObjectDeletion([footprintId]).WithUrbanElementAddition(addBuildingStep).Run()`
- Input curve is deleted by `WithRhinoObjectDeletion` (not preserved in the doc)
- No duplicate detection for now (input curve is destroyed, same ObjectId can never match again)
- Only footprint+height+count+use variant; Brep/parcel variants deferred

### Future Commands (non-exhaustive, deferred)
- `AddBuildingFromBrep(Brep Envelope, double FloorHeight)`
- `AddBuildingOnParcel(Brep Envelope, double FloorHeight)`
- `AddBuildingOnParcelEdge(Brep Envelope, double FloorHeight)`
- `ChangeUse(Group | Parcel2D Parcel, Use Use)`
- `AddZone(Curve Boundary, string Name)`

## Relationship to Core
Commands collect user input, then delegate to `RhinoUrbanModelPipeline` which calls into Core's domain logic. Core owns all model state and computation. The RhinoPlugin is a thin presenter/controller layer on top.

## Rhino Output Conventions

### Layer Structure
All baked geometry follows a consistent nested layer hierarchy:
```
AIQ Urban
  ├── Transit
  │   ├── Road
  │   │   ├── Centerline
  │   │   └── Outline
  │   └── ...
  ├── Building
  │   ├── Envelope
  │   └── FloorPlates
  │       ├── Residential   (one per distinct Use)
  │       └── ...
  ├── Parcel
  ├── Vegetation
  └── Topography
```
Each element type bakes into its corresponding child layer under `AIQ Urban`.
Layers are created on first use if they don't exist.

Floor plate outlines group by Use across all buildings — all "Residential" plates from all buildings share one sublayer.
