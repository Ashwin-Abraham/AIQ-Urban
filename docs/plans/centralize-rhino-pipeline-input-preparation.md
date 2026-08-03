# Plan: Centralize Rhino Urban pipeline input preparation

## Goal

Enforce [ADR 4](../adr/0004-single-input-preparation-boundary.md): every production `RhinoUrbanModelPipeline` invocation receives exactly one Input Preparation Step, and no trigger, event handler, or pipeline configuration constructs or injects `ICreationData` directly.

## Target boundary

The pipeline owns one required Input Preparation Step. The trigger passes raw IDs, coordinate DTOs, and scalar/user parameters to the preparation-step constructor. When the pipeline runs, the step receives the Rhino document, Urban Model, logger, and tolerance through a preparation context; it resolves source state and returns one `List<ICreationData>`.

`ICreationData` remains the common marker and heterogeneous list type. Each concrete type has explicit semantics and consumers. Rhino lifecycle types are consumed by Rhino synchronization; Core model-operation types are consumed by their corresponding Core steps. Core steps do not consume `RhinoObjectDeletionData` as a substitute for a typed domain deletion contract.

## Production trigger audit

### Rhino commands

| Trigger | Current preparation | Migration |
|---|---|---|
| `AddTransit_AIQUrban` | Uses `SelectTransitCenterlinesStep`, but the command resolves `RhinoObject`s and configuration constructs deletion data | Pass selected object IDs, width, and type to `AddTransitInputPreparationStep` |
| `AddBuilding_AIQUrban` | Runs the gather step eagerly, derives deletion IDs outside the pipeline, then runs the gather step again | Pass selected object ID and building parameters to `AddBuildingInputPreparationStep`; run preparation once |
| `AddZone_AIQUrban` | Validates name in the command, runs gathering eagerly, derives deletion IDs, then runs gathering again | Pass object ID and raw name to `AddZoneInputPreparationStep`; move source validation and name resolution into preparation, keep uniqueness as a Core invariant |
| `ChangeBuildingUse_AIQUrban` | Resolves selected geometry IDs and `Use`, then configuration constructs `BuildingUseChangeData` | Pass selected IDs and use identifier to `ChangeBuildingUseInputPreparationStep` |
| `ResetUrban_AIQUrban` | Has no preparation and invokes `ResetUrbanElementsStep` | Rename to `DeleteAllUrbanElements_AIQUrban`; use `DeleteAllUrbanElementsInputPreparationStep` and ordinary typed removal steps |
| `StartUI_AIQUrban` | Uses `DocLoadGatherStep`; `WithDocModelLoad` separately computes Rhino deletion IDs and registers reset behavior | Replace with `LoadDocumentInputPreparationStep`, which prepares load data plus explicit preservation/cleanup contracts |

### HTTP UI messages

| Trigger | Current preparation | Migration |
|---|---|---|
| `TransitMessage` | Uses a gather step but resolves selected `RhinoObject`s in the handler | Pass selected IDs and road width to `AddTransitInputPreparationStep` |
| `AddParcelBoundaryMessage` | Uses a gather step but resolves selected objects in the handler | Pass selected IDs to `AddParcelBoundariesInputPreparationStep` |
| `BuildingMessage` | Runs gather/resolution eagerly and injects prepared data | Use one `AddOrUpdateBuildingInputPreparationStep` for Footprint and Mass modes; keep the envelope resolver internal |
| `AddZoneMessage` | Runs gathering eagerly to derive deletion IDs and also registers the same gather step | Use `AddZoneInputPreparationStep` once |
| `ChangeUseMessage` | Resolves `Use`, constructs parcel/building change data, and injects both types | Use one `ChangeUseInputPreparationStep` that may emit both authoritative contracts |
| `LanduseListEditMessage` | Parses edit type, colour, and ID before configuration constructs `UseModificationData` | Pass raw strings to `ModifyUseListInputPreparationStep` |

`ToolSelectionMessage` and `GetInstallationIdMessage` do not mutate the Urban Model and are outside this pipeline boundary.

### MCP tools

| Trigger | Current preparation | Migration |
|---|---|---|
| `add_transit` | Converts coordinates to Core curves and constructs `TransitData` before the pipeline | Pass coordinate DTOs, width, and type to `AddTransitFromCoordinatesInputPreparationStep` |
| `add_building` | Resolves Use, creates polygons, and constructs `BuildingFootprintData` before the pipeline | Pass coordinate DTOs and building parameters to `AddBuildingFromCoordinatesInputPreparationStep` |
| `add_zone` | Creates a polygon and `ZoneData` before the pipeline | Pass coordinates and name to `AddZoneFromCoordinatesInputPreparationStep` |
| `delete_transit`, `delete_building`, `delete_zone` | Configuration methods construct deletion contracts from IDs | Use operation-specific deletion preparation steps; add authoritative `ZoneDeletionData` |
| `change_building_use` | Resolves Use before configuration constructs change data | Use `ChangeBuildingUseInputPreparationStep` |

Read-only MCP tools and `compute_metrics` do not invoke `RhinoUrbanModelPipeline` and are outside this input-preparation migration.

### Rhino document events

| Trigger | Current preparation/validation in event handling | Migration |
|---|---|---|
| Transit, Building, Zone, Parcel Boundary deletion | Handler resolves the owning element and passes geometry IDs to configurations | Event handler passes raw event ID to a typed deletion preparation step |
| Transit replacement | Handler resolves/normalizes the curve and chooses update versus restore | `ReplaceTransitInputPreparationStep` owns lookup, normalization, and update/rejection contracts |
| Building replacement | Handler extracts the Brep, old floor-plate Uses, and floor height | `ReplaceBuildingInputPreparationStep` owns all resolution and aggregate preparation |
| Floor Plate replacement | Handler extracts the curve and new Z | `ReplaceFloorPlateInputPreparationStep` owns resolution and validation |
| Zone replacement | Handler extracts an unvalidated curve | `ReplaceZoneInputPreparationStep` validates and prepares the update |
| Parcel Boundary replacement | Handler normalizes the curve and directly restores output when invalid | `ReplaceParcelBoundaryInputPreparationStep` emits update or rejection/restoration contracts |
| Derived Parcel replacement | Handler directly deletes the edit and re-bakes the Parcel without a pipeline | Route reconciliation through `RejectParcelReplacementInputPreparationStep` so validation/restoration is not event-handler-owned |

Add events are queued but do not currently dispatch a pipeline mutation. Selection events, document lifecycle events, and UI websocket publication are orchestration concerns and do not require Operation Data.

## Implementation phases

### 1. Establish the enforced API

- Rename `IRhinoDataGatherStep` to `IInputPreparationStep` and `WithDataGatherStep` to the final preparation API.
- Add an `InputPreparationContext` containing `RhinoDoc`, `UrbanModel`, tolerance, and logging dependencies.
- Require exactly one preparation step when constructing a pipeline; execute it once before any Core step.
- Remove production access to `WithCreationData` and `WithRhinoObjectDeletion`.
- Keep a test-only fake preparation step for pipeline orchestration tests; Core-step tests continue passing constructed data directly to Core.

### 2. Make contracts authoritative

- Add `ZoneDeletionData`; change `RemoveZoneStep` to consume only it.
- Change `RemoveTransitStep` to consume only `TransitDeletionData`.
- Keep Rhino deletion/preservation/rejection types as `ICreationData`, but consume them only in Rhino synchronization.
- Add explicit rejection/restoration data needed for per-aggregate partial success instead of relying on exceptions or handler-side output.
- Document each concrete type's consumers in code and cover routing with tests.

### 3. Migrate existing gather paths

- Convert selection-based steps to accept raw Rhino object IDs rather than resolved `RhinoObject`s.
- Fold `BuildingEnvelopeCreationDataResolver` into the building preparation boundary.
- Remove every eager `gatherStep.Run(doc)` call from commands and HTTP handlers.
- Ensure every selected source object receives a consume, preserve, or reject contract.

### 4. Migrate event handling

- Reduce `RhinoUrbanEventHandler` to capture, queue, deduplicate, correlate enough to select an operation, and dispatch raw event handles.
- Move object lookup, geometry extraction, normalization, managed-element resolution, and rejection/restoration decisions into preparation steps.
- Route the two current direct-output recovery paths—invalid Parcel Boundary replacement and any Parcel replacement—through the pipeline.

### 5. Migrate MCP, document load, and delete-all

- Move coordinate conversion and Use resolution from `UrbanModelTools` into preparation steps.
- Replace `DocLoadGatherStep` and `WithDocModelLoad` split ownership with one load preparation boundary.
- Add `DeleteAllUrbanElementsInputPreparationStep`, emitting Transit, Building, Zone, and Parcel Boundary deletion contracts.
- Remove `ResetUrbanElementsStep`; compose ordinary removal steps, then recompute/clear transit surfaces, parcels, graph, and metrics while preserving custom Uses.

### 6. Remove legacy paths and verify

- Make pipeline configurations data-free: they register Core steps, outputters, cascades, metrics, and post-actions only.
- Search production code for direct `new ...Data`, `WithCreationData`, `WithRhinoObjectDeletion`, and eager preparation execution outside preparation-step implementations.
- Add contract tests for source validation versus Core invariants, per-aggregate partial success, explicit rejection/preservation, and all trigger families.
- Update stale pipeline and command descriptions in both Urban context documents after the code migration.
- Run the focused Core and Rhino interface test projects and the full solution tests.

## Completion criteria

- Every production pipeline construction supplies exactly one Input Preparation Step.
- No production trigger, event handler, MCP tool, or pipeline configuration constructs `ICreationData`.
- No event handler validates or normalizes geometry.
- No Core model step consumes Rhino lifecycle data to infer domain intent.
- Delete All Urban Elements follows ordinary typed removal paths and preserves custom Uses.
- Independent valid aggregates still succeed when another aggregate in the same trigger is rejected.
