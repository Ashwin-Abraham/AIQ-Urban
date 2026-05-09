# AIQ.Urban.RhinoPlugin — Context

## Purpose
Rhino 8 plugin providing native commands for urban site modelling. Acts as the bridge between Rhino's document/interaction model and the domain in `AIQ.Urban.Core`.

## Design Constraints
- References `AIQ.Urban.Core` for all domain logic and geometry kernel
- References `RhinoCommon` directly for Rhino UI (doc, views, selection)
- All initial interaction is via native Rhino commands (no UI yet)
- Vue UI via websocket/HTTPS is a future concern

## Command Surface

### First Command: `AddTransit_AIQUrban`
- User picks a planar curve at consistent Z as centerline (Z variation validated in command and rejected with error)
- Prompts: `GetDouble` for width (default 7.0), `GetInteger` for TransitType (0=Road…)
- Calls `UrbanModel.Instance.AddTransit(transit)` which returns void
- Reads `Transit.Outline` and bakes to Rhino doc on layer `AIQ Urban::Transit`

### Future Commands (non-exhaustive)
- `AddBuildingFromFootprint(Curve Footprint, double FloorHeight, int FloorCount)`
- `AddBuildingFromBrep(Brep Envelope, double FloorHeight)`
- `AddBuildingOnParcel(Brep Envelope, double FloorHeight)`
- `AddBuildingOnParcelEdge(Brep Envelope, double FloorHeight)`
- `ChangeUse(Group | Parcel2D Parcel, Use Use)`
- `AddZone(Curve Boundary, string Name)`

## Relationship to Core
Commands call into Core's domain logic; Core owns all model state and computation. The RhinoPlugin is a thin presenter/controller layer on top.

## Rhino Output Conventions

### Layer Structure
All baked geometry follows a consistent nested layer hierarchy:
```
AIQ Urban
  ├── Transit
  ├── Building
  ├── Parcel
  ├── Vegetation
  └── Topography
```
Each element type bakes into its corresponding child layer under `AIQ Urban`.
Layers are created on first use if they don't exist.
