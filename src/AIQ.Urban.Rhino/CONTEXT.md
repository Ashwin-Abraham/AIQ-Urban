# AIQ.Urban.RhinoPlugin — Context

## Purpose
Rhino 8 plugin providing native commands for urban site modelling. Acts as the bridge between Rhino's document/interaction model and the domain in `AIQ.Urban.Core`.

## Design Constraints
- References `AIQ.Urban.Core` for all domain logic and geometry kernel
- References `RhinoCommon` directly for Rhino UI (doc, views, selection)
- All initial interaction is via native Rhino commands (no UI yet)
- Vue UI via websocket/HTTPS is a future concern

## Initial Command Surface (non-exhaustive)
- `AddTransit(bool isFlat, TransitType Type)`
- `AddBuildingFromFootprint(Curve Footprint, double FloorHeight, int FloorCount)`
- `AddBuildingFromBrep(Brep Envelope, double FloorHeight)`
- `AddBuildingOnParcel(Brep Envelope, double FloorHeight)`
- `AddBuildingOnParcelEdge(Brep Envelope, double FloorHeight)`
- `ChangeUse(Group | Parcel2D Parcel, Use Use)`
- `AddZone(Curve Boundary, string Name)`

## Relationship to Core
Commands call into Core's domain logic; Core owns all model state and computation. The RhinoPlugin is a thin presenter/controller layer on top.
