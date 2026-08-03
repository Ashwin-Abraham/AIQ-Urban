# AIQ.Urban.RhinoPlugin — Context

## Purpose
Rhino 8 plugin (`AIQ.Urban.RhinoPlugin.rhp`) owning the native Rhino commands and the document event handler that bridges Rhino's interaction model with the domain in `AIQ.Urban.Core`.

## Agent Tooling

Graphify is forbidden in this context. Do not run `graphify query`, `graphify path`, `graphify explain`, `graphify update`, or any other Graphify command when working on `AIQ.Urban.Rhino`.

## Design Constraints
- **Nullable reference types enabled** — `?` marks values that can legitimately be null; unadorned types are assumed non-null and need not be checked.
- References `AIQ.Urban.RhinoInterface` for pipeline orchestration
- References `RhinoCommon` directly for Rhino UI (doc, views, selection)
- Pipeline supports post-actions via `.WithUrbanModelPostAction(Action<UrbanModel>)` — registered actions execute after all steps, deletions and baking complete.
- **Commands for creation, events for mutation/deletion**: explicit Rhino commands (`AddTransit_AIQUrban`, `AddBuilding_AIQUrban`, etc.) are used to create urban elements. Document events (`ReplaceRhinoObject`, `DeleteRhinoObject`) handle modification and deletion after the fact. A user drawing geometry on a managed layer does NOT automatically create an urban element — that requires an explicit command.
- **RhinoUrbanEventHandler**: instance class implementing `IDisposable`, owned by `Urban_RhinoPlugin`. Subscribes to RhinoDoc events in its constructor, unsubscribes in `Dispose()`. Lives in `AIQ.Urban.RhinoPlugin` (not `RhinoInterface`) because it manages RhinoDoc interaction lifecycle.
- **Deferred event processing**: events are queued into lists (add/remove/replace), then processed on `RhinoApp.Idle` to coalesce multiple Rhino events from a single user action. An `EventLock` guard suppresses event capture while the pipeline writes geometry back to Rhino, preventing re-entrant feedback loops.
- **Events subscribed**: `ReplaceRhinoObject` (modification), `DeleteRhinoObject` (deletion), `SelectObjects`, `DeselectObjects`, `DeselectAllObjects` (selection snapshots), `BeginOpenDocument` / `EndOpenDocument` (file load lifecycle), `RhinoApp.Idle` (processing trigger). **Not** subscribed: `AddRhinoObject` (objects are only added via explicit commands). `UndeleteRhinoObject` deferred for now.
- **Reverse-lookup dictionary**: `Dictionary<Guid, IUrbanElement>` maintained alongside `UrbanModel`, keyed by Rhino ObjectId. Updated when the pipeline bakes new geometry or deletes elements. Used by the event handler to correlate a modified/deleted Rhino object to its owning `IUrbanElement`.
- **Layer filtering**: the event handler checks each object's layer path. Only objects on layers starting with `AIQ Urban::` are processed. Non-urban geometry is ignored.
- **Reverse-lookup dict lifecycle**: the handler owns the dict and wraps each pipeline run. After `pipeline.Run()` returns, the handler iterates the added/deleted GUIDs to update the dict. On `ReplaceRhinoObject`, the handler swaps old ObjectId → new ObjectId in the dict.
- **Error on miss**: reverse-lookup miss is silently skipped. The dict is correctly maintained during a session; after file open there's nothing to do (accepted gap).
- **File open gap**: on `EndOpenDocument`, UrbanModel is empty (in-memory singleton reset). The reverse-lookup dict is also empty. No reconciliation pass yet — user must re-run commands. Accepted gap for v1.
- **HTML UI ownership**: AIQ.Urban.RhinoPlugin owns its own minimal HTML UI infrastructure under `HTML_UI`; copy patterns from the old `ArchSitePlan.RhinoInterface.HTML_UI` code, but do not depend on old `ArchSitePlan.*` UI projects.
- **HTTP POST message contract**: HTTP POST requests from the Vue UI are routed by `messageType` field via `MessageConverter` to typed `IMessageFromUI` classes. Each class implements `HandleMessage()` which runs on the Rhino UI thread via `RhinoApp.InvokeOnUiThread()`. Responses are serialized as `IMessageToUI` JSON objects. See `docs/plans/implement-ui-post-handler.md`.
- **UI geometry source**: UI POST requests carry parameter values only (width, floor height, etc.). Geometry (curves, Breps) comes from pre-selected Rhino objects at the time the POST arrives, obtained via `doc.Objects.GetSelectedObjects()`.
- **Vue UI dist**: the built Vue application under `src/UI/dist`; this is the canonical source of UI assets packaged by the Rhino plugin.

## Event Handler Lifecycle
- Created in `Urban_RhinoPlugin.OnLoad()` → calls `SubscribeModelEvents()` on the singleton handler
- Disposed in `Urban_RhinoPlugin.OnShutdown()` → calls `Dispose()` which unsubscribes all events
- `EventLock` guards against event processing during file loads, pipeline output, and bulk operations
- `ModelChanged` event fires at the end of each non-empty `ProcessQueue` batch after `_doc.Views.Redraw()`. `Urban_RhinoPlugin` subscribes and publishes `ModelStateMessage` over WebSocket so the UI stays in sync with Rhino-side model changes.

## RhinoUrbanModelPipeline
Fluent pipeline in `AIQ.Urban.RhinoInterface/Pipeline/RhinoUrbanModelPipeline.cs`. Data gathering and model execution are separate ordered phases:

```
new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithDataGatherStep(inputGatherStep)           // validates and stages model/Rhino edit data
    .WithDataGatherStep(additionalGatherStep)      // appends another independent data source
    .WithUrbanElementStep(modelStep)               // consumes the aggregated edit-data list
    .Run();
```

- **WithDataGatherStep**: Appends an `IRhinoDataGatherStep`. Gatherers run in registration order and their `IModelEditData` results are concatenated without deduplication.
- **WithUrbanElementStep**: Appends an `IUrbanModelStep` to the ordered execution list.
- `RhinoObjectPreservationData` overrides staged `RhinoObjectDeletionData` for the same ID across the complete gathered operation.
- Pipeline configuration extensions are execution recipes only and never accept edit data or Rhino deletion IDs.
- **Run()**: Gathers the complete edit-data list, executes each model step in configuration order, then batch-deletes and bakes Rhino geometry using the existing outputters.
- Interactive Rhino selections accept valid geometry per object and retain rejected source objects for correction. MCP `add_transit` and `add_building` batches are atomic because they have no retained source geometry: one invalid item rejects the complete request and identifies that item in the failure response.
- If every object in an interactive selection is rejected, the operation is a warning-only no-op: it throws no exception, retains every rejected source object, and creates no urban elements. Each rejection is logged against its Rhino object ID and the gatherer logs accepted/rejected totals.

Element type output is automatic — `OutputTransits()`, `OutputBuildings()`, `OutputParcels(addedIds)`, and `OutputZones(addedIds)` are called by the pipeline for any added elements of those types.

## Testing Strategy

The event handler is tested at two levels:

1. **Unit tests** (`AIQ.Urban.Core.Tests`, no Rhino dependency): an extracted `EventQueue` class handles queuing, deduplication, and dispatch routing. Tested with direct method calls — no Rhino doc needed.
2. **Integration tests** (`tests/AIQ.Urban.RhinoPlugin.Tests`, headless Rhino via `[RhinoTestFixture]`): `RhinoUrbanEventHandler` is wired to a real `RhinoDoc`; programmatic add/delete/replace operations verify end-to-end queuing → pipeline dispatch → UrbanModel state.

Core steps (`RemoveBuildingStep`, etc.) follow the existing step-testing pattern in `AIQ.Urban.Core.Tests`.

## RhinoUrbanEventHandler — Dispatch Logic

The event handler dispatches `DeleteRhinoObject` and `ReplaceRhinoObject` events by element type. Buildings are skipped for Replace (deferred to later stage).

The handler checks the modified object's layer to decide if it's authoritative geometry (Centerline for Transit, Boundary curve for Zone). Objects on derived layers (Outline, Surface, Envelope, FloorPlates) are re-baked on pipeline runs and user modifications are overwritten — that is correct behaviour.

When a Transit centerline replacement violates either the ground-plane or non-self-intersection invariant, the replacement is rejected individually: the invalid Rhino curve is deleted, the Transit's last valid centerline and outline are re-baked from `UrbanModel`, its width and type are preserved, and a warning is logged.

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
- Pipeline: selection gatherer → `WithTransitAdditionSteps()` → full model output.
- `TransitSurfaceUnioner` is registered as an `IUrbanModelStep` — unions all transit outlines per type into `UrbanModel.TransitSurfaces`
- `RecalculateParcelsStep` runs after `TransitSurfaceUnioner` — reads `UrbanModel.TransitSurfaces` holes to create parcels
- Valid input curves stage their own Rhino deletion; rejected curves stage no edit or deletion and remain untouched.
- Duplicate detection via `RemoveTransitStep`: if same centerline ObjectId was previously used, old Transit is removed from UrbanModel and baked outline deleted from Rhino doc
- **Known gap**: When transit is deleted/modified, previously baked unioned `Surface` geometry remains as orphans in the Rhino doc — no cleanup yet. Parcel geometry for old parcels is correctly cleaned up since `RecalculateParcelsStep` returns old parcel GUIDs as `IDeleteUrbanElementsStep` output.

### `AddBuilding_AIQUrban`
- Two modes selected by `manual_generationType` ("Footprint" or "Mass")
- **Footprint mode**: collects footprint curves, floor height, floor count, and Use. `SelectBuildingFootprintStep` validates each input and stages `BuildingFootprintData` with its Rhino deletion.
- **Mass mode**: collects Breps or Extrusions, floor height, and Use. `SelectBuildingEnvelopeStep` validates each input, resolves add-versus-update against `UrbanModel`, and stages the edit with its Rhino deletion.
- `AddBuildingStep` discriminates by edit-data type (`BuildingFootprintData` vs `BuildingMassData`) and calls the appropriate `Building` constructor.
- Accepted input geometry is deleted and re-baked on the proper layer; rejected input remains untouched.
- No duplicate detection for now (input geometry is destroyed, same ObjectId can never match again)

### `AddZone_AIQUrban`
- User picks a closed planar curve as the zone boundary, then enters a zone name via text prompt
- Validates: curve is closed and planar, name does not contain `::`, name is not already taken by an existing zone
- Pipeline: `SelectZoneBoundaryStep` → `WithZoneAdditionSteps()` → full model output.
- The gather step stages the accepted input curve's deletion with `ZoneData`.
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

### `StartUI_AIQUrban`
- Starts the AIQ Urban HTML UI from Rhino.
- **Model loading**: Before starting UI servers, scans the Rhino doc and hydrates `UrbanModel` from existing baked geometry. Uses `WithDocModelLoad()` pipeline configuration. UrbanModel is nuked (`Reset()`) and rebuilt from scratch. Loading order: uses → transits (from centerlines) → transit surface union → parcels (with use preservation from doc centroids) → buildings (from envelopes + floor plates) → zones → metrics. A managed Transit centerline that violates the ground-plane invariant is excluded from `UrbanModel`, logged with its Rhino object ID, and left untouched in the document for correction. Other orphaned geometry is silently skipped.
- Command owns UI startup only: starts local HTTP/static-file serving, starts websocket communication, then opens an Eto WebView pointed at the served `index.html`.
- Local UI ports follow the old working pattern: HTTP on `localhost:6300`, websocket on `localhost:6400`.
- Static UI assets are copied from the Vue UI dist into the plugin output under `HTML_UI/wwwroot` at build time; the HTTP server serves from the copied output directory, not the source tree.
- The Rhino plugin build does not run the Vue build; if the Vue UI dist is missing, the plugin build fails with `AIQ Urban UI not found.`
- UI servers are not started on plugin load; the UI is optional and ports are only bound after the user runs this command.
- Closing the UI window stops both local servers so ports are released and the command can be run again cleanly.
- Only one UI window should be open at a time; re-running the command should focus/reuse the existing window or no-op successfully, not create duplicate windows or duplicate servers.
- Uses the new AIQ.Urban.RhinoPlugin HTML UI infrastructure, copied from the old `ArchSitePlan.RhinoInterface.HTML_UI` pattern without depending on the old project.
- Websocket serving uses `WebSocketSharp`, matching the old working Rhino UI pattern for the first pass.
- UI-to-Rhino commands over HTTP `POST` use a typed message contract (`MessagesFromUI/`, `MessageToUI/`). Incoming JSON is deserialized by `MessageConverter`, routed by `messageType` field, and executed on the Rhino UI thread. Domain changes go through `RhinoUrbanModelPipeline` — never directly to Core.
- Implemented message types: `Transit`, `Building`, `Zone`, `Landuse`. See `docs/plans/implement-ui-post-handler.md` for the original base plan.
- `ToolSelection` messages are received but not yet acted on (deferred).
- CORS headers (`Access-Control-Allow-Origin: *`) are added to all HTTP responses for compatibility.

### Deferred Commands (non-exhaustive)
- `AddBuildingOnParcel(Brep Envelope, double FloorHeight)`
- `AddBuildingOnParcelEdge(Brep Envelope, double FloorHeight)`

### ProjectMetricsJsonSerializer
Serializer in `Pipeline/ModelActions/ProjectMetricsJsonSerializer.cs`. Reads `UrbanModel.ProjectMetrics` and writes it as indented JSON to `%USERPROFILE%\Documents\AIQ Urban\{RhinoDocName}-project-metrics.json`. Invoked as a `WithUrbanModelAction` step after the pipeline computes metrics. Uses `System.Text.Json` (no additional dependencies).

## UI-to-Rhino POST Message Contract

### Incoming Messages (`MessagesFromUI/`)
Each implements `IMessageFromUI<T>` with a `HandleMessage()` method that returns `IMessageToUI`.

| Message | `messageType` | Source (Vue component) | Parameters | Geometry Source |
|---|---|---|---|---|
| `TransitMessage` | `"Transit"` | `TransitTool.vue` | `RoadWidth` | Selected curves in RhinoDoc |
| `BuildingMessage` | `"Building"` | `BuildingTool.vue` | `FloorCount`, `FloorHeight`, `manual_generationType` ("Footprint" or "Mass") | Selected curves (Footprint) or Breps (Mass) |
| `AddZoneMessage` | `"Zone"` | `ZonesTool.vue` | `name` | Selected closed curve in RhinoDoc |
| `ChangeUseMessage` | `"Landuse"` | `LanduseTool.vue` | `use` | Selected parcel outlines, building envelopes, or floor-plate curves in RhinoDoc |
| `ToolSelectionMessage` | `"ToolSelection"` | `ModellingTools.vue` | `Tool` (name) | None (stub) |
| `GetInstallationIdMessage` | `"GetInstallationId"` | On-demand UI caller | None | None |

### Outgoing Messages (`MessageToUI/`)
Returned as JSON in HTTP responses or published to the UI websocket. Both transports use the
same camelCase JSON field names, and the UI routes by the `type` field.

| Message | `type` | Content |
|---|---|---|
| `ModelStateMessage` | `"ModelStateMessage"` | Landuses (from `DefaultUses.All`) + per-zone chart metrics (from `ProjectMetrics`) |
| `EmptyMessage` | `"EmptyMessage"` | No content (used for stubs) |
| `InstallationIdMessage` | `"InstallationIdMessage"` | `installationId`, the persisted AIQ Urban Installation ID |
| `ErrorMessage` | `"Error"` | `message`, returned when a handler fails |

## Installation Identity

**Installation ID**:
An application-generated UUID v4, represented by its canonical hyphenated string, that identifies one AIQ Urban installation and remains stable across Rhino and plugin upgrades while its persisted application data remains intact.
_Avoid_: Machine ID, Device ID, Hardware ID

The Installation ID does not derive from operating-system or hardware identifiers and may change after an operating-system reinstall or deliberate removal of AIQ Urban's persisted application data.

The Installation ID is scoped to the operating-system user and persisted as `installation-id` beneath an `AIQ Urban` directory in the user-local application-data location resolved through .NET. Different operating-system users on the same device therefore receive different Installation IDs; persistence must not require administrator privileges.

The storage location is unversioned. AIQ Urban plugins hosted by different Rhino versions share the same Installation ID for the same operating-system user.

On first access, a missing Installation ID is generated and persisted. An empty or malformed persisted value is treated as lost application data: AIQ Urban logs a warning, replaces it with a newly generated UUID, and returns the replacement rather than failing the UI request.

An Installation ID is returned only after persistence succeeds. If the local application-data directory cannot be read or written, the HTTP request returns the existing `ErrorMessage` contract rather than an ephemeral ID that could change on the next request. This feature preserves the HTTP server's current handler-error response and status-code behavior.

`GetInstallationIdMessage` follows the standard UI-to-Rhino POST dispatch and executes on Rhino's UI thread. The HTTP server does not special-case this non-document request.

The persisted Installation ID is the sole source of truth. It is read and validated on every request rather than cached in plugin memory.

Installation ID retrieval is lazy: AIQ Urban performs it only when the UI sends `GetInstallationId` through the existing HTTP POST pathway. The plugin does not fetch or push the ID during UI startup. Retrieval failures return `ErrorMessage` to the requesting UI call.

## Selection Metrics

**Selected Urban Element**:
A selected user-facing Building, Parcel, or Transit whose aggregate metrics are shown in the UI.
_Avoid_: treating derived transit surfaces, parcel boundaries, unmanaged Rhino geometry, or individual floor plates as separate selected element categories.

**Selection Metrics Snapshot**:
A complete point-in-time summary containing non-null Building, Parcel, and Transit aggregates, including zero-count aggregates for unselected categories.
_Avoid_: event deltas, legacy `SelectedObjectMetricsMessage` compatibility emissions, or choosing one selected category according to the active UI tool.

### Websocket Message Contract

| Message | `type` | Content |
|---|---|---|
| `SelectionMetricsMessage` | `"SelectionMetricsMessage"` | Typed `buildings`, `parcels`, and `transits` aggregates filtered from the current `ProjectMetrics` by `SourceElementId` |
| `ModelStateMessage` | `"ModelStateMessage"` | Landuses (from `DefaultUses.All`) + per-zone chart metrics (from `ProjectMetrics`). Published after each non-empty `ProcessQueue` batch so the UI stays in sync when model changes originate from Rhino viewport interactions. |

### Relationships

- A selected **FloorPlate** contributes to selection of its owning **Building**.
- All **Selected Urban Elements** produce one **Selection Metrics Snapshot** over the UI websocket.
- Every select, partial-deselect, and deselect-all event produces the complete, deduplicated current snapshot.
- After document loading, Rhino caches the current model state and selection snapshots before the UI websocket accepts connections.
- A newly connected UI websocket client immediately receives the current model state snapshot followed by the current selection snapshot.
- A selected Parcel's building-dependent metrics include every Building contained by that Parcel; the Building aggregate includes only explicitly selected Buildings.
- Across multiple selected Parcels, FAR and coverage are calculated from combined totals.
- Zone aggregation remains in `ModelStateMessage`; its synthetic `"All"` zone uses `SourceElementId = Guid.Empty`.
- Selection aggregate records belong to the Rhino UI transport contract; Core remains unaware of websocket messages and Rhino selection state.
- UI selection messages broadcast only on websocket path `/`; `/mcp-bridge` does not receive them.
- A non-empty `ProcessQueue` batch publishes `ModelStateMessage` followed by a recomputed `SelectionMetricsMessage`.
- A successful UI POST returns `ModelStateMessage`, refreshes its websocket cache without echoing it, and publishes a recomputed `SelectionMetricsMessage`.

### TransitMessage Handling
1. Get `RhinoDoc.ActiveDoc`
2. Get selected curve objects, filter to `ObjectType.Curve`
3. During pipeline data gathering, validate each selected curve independently against the ground-plane invariant. For each valid curve, produce `TransitData` plus Rhino-object deletion data; for each invalid curve, log a warning and produce no edit or deletion data, leaving that Rhino curve untouched.
4. Pipeline: `WithTransitAdditionSteps()` + `WithMetricsCalculation()` via `RhinoUrbanModelPipeline`
5. Return `ModelStateMessage`

### BuildingMessage Handling
Two modes controlled by `manual_generationType`:

- **Footprint**: Get selected curves → `SelectBuildingFootprintStep` produces `BuildingFootprintData(polygon, FloorHeight, FloorCount, DefaultUses.Undefined)`
- **Mass**: Get selected Breps/Extrusions → `SelectBuildingEnvelopeStep` produces `BuildingMassData(envelopeBrep, FloorHeight, DefaultUses.Undefined)`. The step contours the envelope horizontally at intervals of `FloorHeight` to derive floor plate outlines. Contours with curve length < tolerance are discarded. `FloorCount` = `Floor(envelopeHeight / FloorHeight)`.
- In Mass mode, a resolver classifies every selected envelope before model mutation. An unmanaged envelope produces `BuildingMassData` for `AddBuildingStep`. A managed Building envelope produces `BuildingUpdateData` for `UpdateBuildingStep`, replacing the old Building and its floor plates while mapping the existing floor-plate Uses onto the replacement plates.
- For a managed envelope, `BuildingUpdateData.TypicalFloorHeight` comes from the new `BuildingMessage.FloorHeight`; the old Building contributes its ordered floor-plate Uses, not its previous floor height.
- Both add and update steps run in the same pipeline, ignore absent edit-data types, and support multiple selected envelopes in one operation.
- Only a managed Building envelope is authoritative for an update. Selecting a floor plate never produces `BuildingUpdateData` and does not remove or update its owning Building.
- Pipeline: resolver → `WithBuildingAddOrUpdateSteps()` (`AddBuildingStep` + `UpdateBuildingStep`) → full model output → metrics via `RhinoUrbanModelPipeline`
- `AddBuildingStep` discriminates between its supported edit-data types and calls the appropriate `Building` constructor; `UpdateBuildingStep` consumes only `BuildingUpdateData`.
- Building Use always defaults to `DefaultUses.Undefined` (UI does not send useName)

### Thread Safety
All `HandleMessage()` calls are marshalled to the Rhino UI thread via `RhinoApp.InvokeOnUiThread()`. The `HttpListener` runs on thread pool threads — without this marshalling, Rhino doc access would crash.

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
