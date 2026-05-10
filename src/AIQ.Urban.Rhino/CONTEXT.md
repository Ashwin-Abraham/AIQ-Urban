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
Fluent pipeline in `Pipeline/RhinoUrbanModelPipeline.cs`. Steps execute immediately on each call (not deferred), with `BakeNewUrbanElements` as the terminal step:

```
new RhinoUrbanModelPipeline(doc)
  .RemoveMatchingElements(matchIds, exemptFromRhinoDelete)
  .ChangeInputCurvesLayer(curveIds, transitType, width)
  .AddTransit(newTransits)
  .BakeNewUrbanElements()
```

- **RemoveMatchingElements**: Finds `IUrbanElement`s in UrbanModel whose `GetGeometryIds()` intersect `matchIds`. Deletes their non-exempt Rhino objects from the doc, then removes them from UrbanModel.
- **ChangeInputCurvesLayer**: Moves the original input curves to the centerline layer for the given TransitType/width.
- **AddTransit**: Adds to UrbanModel and queues outlines for baking.
- **BakeNewUrbanElements**: Bakes queued outlines to the Rhino doc and overwrites `CurveElement.Id` with the real Rhino ObjectId.

## Command Surface

### `AddTransit_AIQUrban`
- User picks a planar curve at consistent Z as centerline (Z variation validated in command and rejected with error)
- Prompts: `GetDouble` for width (default 7.0), `GetInteger` for TransitType (0=Road…)
- No longer deletes input curves — they are re-layered to the transit centerline layer
- Only the `Outline` is baked (centerline is the original re-layered curve)
- Duplicate detection: if the selected curve was previously used as a centerline, the old Transit is removed from UrbanModel and its baked outline deleted from Rhino doc before the new one is created

### Future Commands (non-exhaustive)
- `AddBuildingFromFootprint(Curve Footprint, double FloorHeight, int FloorCount)`
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
