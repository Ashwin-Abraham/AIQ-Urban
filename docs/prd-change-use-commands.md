## Problem Statement

Currently, once a Parcel, Building, or FloorPlate is created in the urban model, there is no way to change its Use. The only Use-preservation mechanism is the nearest-centroid matching in `RecalculateParcelsStep`, which runs on transit changes and cannot be explicitly controlled. Users need to be able to reassign land uses and built-area uses after initial creation to explore design alternatives without rebuilding the entire model.

## Solution

Two new Rhino commands that allow users to modify the Use of existing model elements in-place:

- **`ChangeBuildingUse_AIQUrban`** — modifies the Use of a Building (all FloorPlates) or individual FloorPlates. Accepts selection of building envelope Breps for building-wide changes and/or floor plate outline curves for per-storey changes.
- **`ChangeParcelUse_AIQUrban`** — modifies the Use of one or more Parcels. Accepts selection of parcel outline curves.

Both commands present a DefaultUses dropdown (matching the existing `AddBuilding_AIQUrban` pattern) for the target Use. Both trigger metrics recalculation after the change.

## User Stories

1. As a site planner, I want to select a parcel in the viewport and change its Use from Public to Residential, so that I can explore alternative land-use layouts.
2. As a site planner, I want to select multiple parcels at once and assign them all the same new Use, so that I can batch-rezone an area.
3. As a site planner, I want to select a building envelope and change its Use from Commercial to MixedUse, so that all floor plates of that building update consistently.
4. As a site planner, I want to change only the ground floor of a mixed-use building to Retail while keeping upper floors as Residential, so that I can model ground-floor retail accurately.
5. As a site planner, I want to select both a building envelope and an individual floor plate of a different building in one command invocation, so that I can make targeted changes efficiently.
6. As a site planner, I want the building-wide change to take priority when I select both a building envelope and one of its own floor plates in the same command run, so that I don't accidentally create conflicting per-plate overrides.
7. As a site planner, I want metrics (AreaPerUse, BuiltAreaByUse, ZoneMetrics) to reflect the new Use assignments immediately after the command completes, so that reports stay consistent.
8. As a site planner, I want the Rhino layer assignment to update automatically — the element moves from the old-use layer to the new-use layer — so that visual organisation stays consistent with the model.
9. As a developer, I want the step logic to be testable without a Rhino doc, so that I can catch regressions in unit tests.

## Implementation Decisions

### Domain Model Changes

- **`Parcel.SetUse(Use use)`** — new method. Mutates the Use in-place. The existing `RecalculateParcelsStep` preserves Use by nearest-centroid matching; explicitly set uses may be overwritten if transit changes reshape parcels significantly. This is accepted behaviour for now.
- **`FloorPlate.SetUse(Use use)`** — new method. Mutates the Use in-place.
- **`Building.Use` property** — removed entirely. The Building constructor still accepts a `Use` parameter to initialise all floor plates, but the value is not retained at the Building level. Each FloorPlate owns its own Use, which is the single source of truth.
- No changes to `UrbanModel`, `Use` record, `DefaultUses`, or the pipeline interfaces.

### Step Pattern (Core, no Rhino dependency)

Both steps implement `IEditUrbanElementsStep` and live in `AIQ.Urban.Core/ModelActions/`:

**`ChangeBuildingUseStep`** — receives a flat `List<Guid>` of selected geometry IDs and a `Use newUse`. At runtime it:
1. Linear-scans `UrbanModel.Buildings`. For each Building, checks if any of its `FloorPlate.Outline.Id` or `Envelope.Id` matches the input GUIDs.
2. Processes envelope matches first (building-wide): calls `SetUse(newUse)` on all FloorPlates of that Building, tracks the Building as "touched".
3. Processes floor plate outline matches (per-plate): calls `SetUse(newUse)` on that FloorPlate, but **skips** FloorPlates whose parent Building was already touched in step 2 (building-wins priority).
4. Returns old and new GUIDs from all affected Buildings (old geometry on old-use layer is deleted, new geometry is baked on new-use layer).

**`ChangeParcelUseStep`** — receives a flat `List<Guid>` of parcel outline geometry IDs and a `Use newUse`. Linear-scans `UrbanModel.Parcels`, matches by `Outline.Id`, calls `SetUse(newUse)` on matching Parcels. Returns old and new GUIDs.

### Pipeline and Rhino Commands

Both commands follow the existing pattern:
- Rhino `Command` subclass in `AIQ.Urban.Rhino/Commands/`
- `GetObject` for viewport selection (curves + Breps for building command, curves for parcel command)
- `go.AddOptionList("Use", DefaultUses.Names, 0)` for Use selection
- Pipeline: `new RhinoUrbanModelPipeline(doc, UrbanModel.Instance).WithUrbanElementStep(step)`
- `PipelineConfigurations.WithMetricsCalculation(saveToFile: true, ...)` after the step
- Always re-bakes the **entire parent Building** when any FloorPlate changes (envelope + all plates). `BuildingOutputter` handles this — no separate `FloorPlateOutputter` is needed.

### GUID Resolution

The step inspects `UrbanModel.Buildings` to distinguish envelope GUIDs from floor plate outline GUIDs:
- If `Guid == building.Envelope.Id` → building-wide
- If any `plate.Outline.Id == Guid` → per-plate
- If neither → silently ignored (graceful no-op for unmatched selections)

For parcels, matching is direct: `parcel.Outline.Id == Guid`.

## Testing Decisions

- **Good test**: exercises the step's behaviour with a populated `UrbanModel` and asserts FloorPlate/Parcel Use values after `Run()`. No Rhino doc needed — the step is pure domain logic.
- **All three test modules will be tested**:
  1. `ChangeBuildingUseStepTests` — unit tests in `AIQ.Urban.Core.Tests/ModelActions/`
  2. `ChangeParcelUseStepTests` — unit tests in `AIQ.Urban.Core.Tests/ModelActions/`
  3. Pipeline integration tests — in `AIQ.Urban.RhinoPlugin.Tests/`

### Prior Art for Tests

- `AddBuildingStepTests.cs` — exercises the step with real `UrbanModel` state, asserts model state after Run()
- `TransitSurfaceAndParcelCalculationTests.cs` — tests derived geometry steps end-to-end through the real dependency chain
- `TransitPipelineConfigurationTests.cs` — full pipeline with headless `RhinoDoc`, asserts Rhino layer counts and model state

## Out of Scope

- Custom (non-DefaultUses) Use creation — users pick from the 9 predefined uses only
- Bulk rezoning by zone or by current-use filter (e.g. "change all Public parcels to Residential")
- Undo/redo support beyond what Rhino provides natively
- Per-storey independent geometry (e.g., different footprint per floor) — the Building constructor still creates identical floor plate shapes
- Changes to the `RecalculateParcelsStep` Use-preservation heuristic (existing centroid-matching behaviour stands)
- Vue UI or websocket interface — native Rhino commands only

## Further Notes

- The `RecalculateParcelsStep` preserves Use by nearest-centroid matching. Parcel Use changes made via `ChangeParcelUse_AIQUrban` will be overwritten if transits are later modified and parcels are reshaped significantly. This is consistent with the existing behaviour (centroid fallback for unmatched parcels) and should be documented in the command's Rhino output.
- `Building.Use` removal is backwards-compatible for new code paths — no existing AIQ.Urban.* code reads `Building.Use`. The old `ArchSitePlan.*` projects reference `FloorPlate.Use` only, not `Building.Use`.
