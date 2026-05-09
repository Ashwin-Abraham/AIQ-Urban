# AIQ.Urban.Core — Domain Context

## Purpose
Pure city-modelling domain logic. Models buildings, transit networks, parcels, zones, topography, and vegetation for architectural site-planning in Rhino.

## Implementation Order (First Command)
1. `Models/ModelElement.cs` — base class with `Guid Id`
2. `Models/Transit.cs` — Transit subclass, Outline calculated eagerly in constructor
3. `UrbanModel.cs` — singleton with `Instance`, typed `List<Transit> Transits`, `AddTransit(Transit): void`
4. `Geometry/` (if needed) — any extracted geometry helpers for Outline calculation
5. Rhino command `AddTransit_AIQUrban` — user interaction + Core call + layer baking

## Design Constraints

### Dependency Rule
- **AIQ.Urban.Core** references RhinoCommon as its geometry kernel (Curve, Brep, Mesh, Point3d, Transform, etc.)
- **AIQ.Urban.RhinoPlugin** references AIQ.Urban.Core + RhinoCommon separately for the Rhino interface
- Core depends on **no** other solution projects (especially not the old `ArchSitePlan.*` or `BHoM.*` projects)

### Model State
- `UrbanModel` is a singleton in AIQ.Urban.Core holding all `ModelElement`s and `ModelGroup`s. It is the single source of truth for the in-memory model. Rhino doc serialization is a future concern.
- Access pattern: static `UrbanModel.Instance` property with lazy initialization.
- Operation methods live directly on `UrbanModel` (e.g. `urbanModel.AddTransit(transit)`).
- Elements are stored in typed lists: `List<Transit> Transits { get; }`, `List<Building> Buildings { get; }`, etc.

### Pipeline
- Eager execution: when a command adds/modifies an element, the pipeline automatically recalculates all dependent elements.
- Example: `AddTransit` adds a road → parcels intersecting that road are automatically split/recalculated.
- RhinoPlugin commands collect user input, convert Rhino types → Core types, call into `UrbanModel`/pipeline, then read back from `UrbanModel` to display.
- **CommandOrchestratorPipeline deferred.** For the first command, baking Rhino geometry into the Rhino doc is handled locally. A shared `CommandOrchestratorPipeline` in the Rhino layer will be extracted later when common cross-command sync behaviours emerge.
- **No `ModelOrchestratorPipeline` class for now.** Operation methods live directly on `UrbanModel` (e.g. `urbanModel.AddTransit(...)`). A separate pipeline class can be introduced later when complex multi-step actions or dependency chains justify it.

### ModelElement Hierarchy
- `ModelElement` is the base type for all urban elements. Properties: `Guid Id` (generated in constructor). Subclasses: Building, Transit, Parcel2D, Parcel3D, Vegetation, TopoMesh, etc.
- `ModelGroup` (aliased as `Group`) — Zone, Parcel, Site — contains `List<ModelElement> Elements` but is **not** itself a ModelElement (no composite nesting)
- `Metrics` subtypes (ZoneMetrics, AreaMetrics, etc.) reference a ModelGroup but are also **not** ModelElements

### Folder Structure
- `Models/` — ModelElement, Transit, and future model types
- `Geometry/` — geometry helper utilities
- Project root — UrbanModel and top-level orchestration

### Geometry Approach
- `Profile2D` is a new custom class guaranteed to operate in XY only (no Z dimension). Used for FloorPlate profiles and Parcel2D outlines.
- `Node` is a new custom class for graph nodes. Future consideration: replace with topologyNet when TransitGraph is fully designed.
- RhinoCommon types (Curve, Brep, Mesh, Point3d, Transform) used for all 3D geometry.

## Domain Glossary

### Use
A semantic label with visual feedback. `string Name` + `Color` (System.Drawing.Color). Not an enum — instances are created at runtime (e.g. "Residential", "Commercial", "Retail", "Industrial", "MixedUse", "Public", "Park", "Parking", "Utility").

### Building
A constructed structure. Has a collection of `FloorPlate`s and a `Brep Envelope` representing the outer shell.

### FloorPlate
A single storey of a Building. Defined by a `Profile2D`, a `double ZHeight`, and a `Use`.

### TransitType
Enum: `Road`, `Railway`, `CyclePath`, `Pedestrian`, `Waterway`.

### Transit
A linear transportation corridor of a given `TransitType`. `Outline` is calculated eagerly in the constructor via a private method. Properties:
- `Curve Centerline` — path along the corridor (must be planar at a consistent Z)
- `Curve Outline` — closed boundary curve formed by offsetting centerline ±width/2 with straight-line end caps representing the full right-of-way
- `double Width`
- `bool IsOnGround`
- `List<TransitNode> OrderedNodes`

### TransitGraph
Collection of `TransitNode`s forming a connected network.

### TransitNode
A `Node` with `List<Node> Neighbours`. Junction/intersection in a transit network.

### Parcel2D
A 2D land parcel with a `Profile2D Outline` and a `Use`.

### Parcel3D
A 3D parcel extruded from a `Parcel2D FlatParcel` plus a `Mesh ProjectedParcel` (e.g. terrain-projected).

### Zone
A `ModelGroup` with a `Polygon Outline` and a `string Name`. Groups ModelElements within a named zone boundary (e.g. "Downtown", "Park").

### Parcel (group)
A `ModelGroup` attached to a `Parcel2D Host`. Groups ModelElements that sit on that parcel.

### Site
A `ModelGroup` — top-level container for the entire project. No extra properties yet.

### ModelGroup
Base grouping container. `List<ModelElement> Elements` + `string Name`.

### TopoMesh
A terrain mesh. Wraps a Rhino `Mesh`.

### Vegetation
A `PointObject` with `Transform Base` and `Geometry GeometryAtOrigin`. A placed plant/object repeated from a template.

### Metrics
Abstract base for computed analytics on a `ModelGroup`. Concrete subtypes:
- **ZoneMetrics**, **AreaMetrics**, **RadiationMetrics**, **TransitMetrics**, **ParcelMetrics**

## Test Projects
- `tests\AIQ.Urban.Core.Tests\` — xUnit tests for AIQ.Urban.Core
- `tests\AIQ.Urban.RhinoPlugin.Tests\` — xUnit tests using `RhinoTest` NuGet package for Rhino-aware tests

## Relationship to Old Solution
The `ArchSitePlan.*` projects (`ArchSitePlan.Core`, `ArchSitePlan.RhinoAddin`, `ArchSitePlan.BHoM.Geometry`, etc.) and `tests\AQI.Urban.Tests` are a past attempt to be ignored and eventually deleted. The new codebase starts fresh in `AIQ.Urban.Core` and `AIQ.Urban.RhinoPlugin`.
