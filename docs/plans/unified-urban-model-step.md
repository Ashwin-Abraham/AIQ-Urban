# Plan: Unified IUrbanModelStep with ICreationData Consumption

## Context

Currently, `IUrbanModelStep` and `IEditUrbanElementsStep` are separate interfaces with different return types (`List<Guid>` vs `EditStepResult`). Delete and edit steps receive their target IDs/parameters via constructor and ignore `creationData` in `Run()`. Only add steps consume creation data from `Run()`.

This refactor unifies all steps into a single `IUrbanModelStep` interface where every step receives `List<ICreationData>` in `Run()` and extracts its parameters from there. New `ICreationData` record types are created for delete and edit operations.

## Design Decisions

- Single `IUrbanModelStep` interface with `StepResult Run(UrbanModel, List<ICreationData>)`
- `record StepResult(List<Guid> AddedIds, List<Guid> DeletedIds)` replaces both `List<Guid>` and `EditStepResult`
- Marker interfaces `IAddUrbanElementsStep`, `IDeleteUrbanElementsStep`, `IEditUrbanElementsStep` deleted
- `EditStepResult` deleted
- Pipeline accumulates creation data (add, not overwrite)
- Steps with configuration params (tolerances) keep them in constructors
- `AffectedUse` property on `ModifyModelUsesStep` dropped (never read externally)
- Parameterless utility steps just change return type

## Implementation Phases

### Phase 1: New types and unified interface

**Files to create/modify:**

1. **`src/AIQ.Urban.Core/ModelActions/IUrbanModelStep.cs`** — Rewrite
   - Remove `IAddUrbanElementsStep`, `IDeleteUrbanElementsStep`, `IEditUrbanElementsStep`, `EditStepResult`
   - Define `record StepResult(List<Guid> AddedIds, List<Guid> DeletedIds)`
   - Define `IUrbanModelStep` with `StepResult Run(UrbanModel model, List<ICreationData> creationData)`

2. **`src/AIQ.Urban.Core/ModelActions/CreationData/BuildingDeletionData.cs`** — Create
   ```csharp
   public record BuildingDeletionData(List<Guid> GeometryIds) : ICreationData;
   ```

3. **`src/AIQ.Urban.Core/ModelActions/CreationData/TransitDeletionData.cs`** — Create
   ```csharp
   public record TransitDeletionData(List<Guid> CenterlineIds) : ICreationData;
   ```

4. **`src/AIQ.Urban.Core/ModelActions/CreationData/ZoneDeletionData.cs`** — Create
   ```csharp
   public record ZoneDeletionData(List<Guid> GeometryIds) : ICreationData;
   ```

5. **`src/AIQ.Urban.Core/ModelActions/CreationData/BuildingUseChangeData.cs`** — Create
   ```csharp
   public record BuildingUseChangeData(List<Guid> GeometryIds, Use NewUse) : ICreationData;
   ```

6. **`src/AIQ.Urban.Core/ModelActions/CreationData/ParcelUseChangeData.cs`** — Create
   ```csharp
   public record ParcelUseChangeData(List<Guid> GeometryIds, Use NewUse) : ICreationData;
   ```

7. **`src/AIQ.Urban.Core/ModelActions/CreationData/TransitUpdateData.cs`** — Create
   ```csharp
   public record TransitUpdateData(Guid OldCenterlineId, Curve NewCenterline) : ICreationData;
   ```

8. **`src/AIQ.Urban.Core/ModelActions/CreationData/ZoneUpdateData.cs`** — Create
   ```csharp
   public record ZoneUpdateData(Guid OldBoundaryId, Curve NewBoundary) : ICreationData;
   ```

9. **`src/AIQ.Urban.Core/ModelActions/CreationData/BuildingUpdateData.cs`** — Create
   ```csharp
   public record BuildingUpdateData(Guid OldEnvelopeId, Brep NewEnvelope, double TypicalFloorHeight, List<Use> OldPlateUses) : ICreationData;
   ```

10. **`src/AIQ.Urban.Core/ModelActions/CreationData/FloorPlateReplaceData.cs`** — Create
    ```csharp
    public record FloorPlateReplaceData(FloorPlate OldPlate, double NewZ) : ICreationData;
    ```

11. **`src/AIQ.Urban.Core/ModelActions/CreationData/UseModificationData.cs`** — Create
    ```csharp
    public record UseModificationData(UseEditType EditType, string Name, Color Color, Guid Id) : ICreationData;
    ```

### Phase 2: Refactor add steps (minimal changes)

These already consume creation data. Only interface and return type change.

12. **`src/AIQ.Urban.Core/ModelActions/AddBuildingStep.cs`** — Modify
    - Change `: IAddUrbanElementsStep` → `: IUrbanModelStep`
    - Change return type from `List<Guid>` (explicit interface impl) to `StepResult` (public)
    - Wrap return in `new StepResult(ids, [])`

13. **`src/AIQ.Urban.Core/ModelActions/AddTransitStep.cs`** — Modify
    - Same pattern as AddBuildingStep

14. **`src/AIQ.Urban.Core/ModelActions/AddZoneStep.cs`** — Modify
    - Same pattern as AddBuildingStep

### Phase 3: Refactor delete steps

Remove constructor params, extract IDs from creation data.

15. **`src/AIQ.Urban.Core/ModelActions/RemoveBuildingStep.cs`** — Modify
    - Remove `List<Guid> _geometryIds` from primary constructor
    - Change `: IDeleteUrbanElementsStep` → `: IUrbanModelStep`
    - In `Run()`: extract IDs from `creationData.OfType<BuildingDeletionData>()`
    - Return `new StepResult([], removedIds)`

16. **`src/AIQ.Urban.Core/ModelActions/RemoveTransitStep.cs`** — Modify
    - Same pattern, using `TransitDeletionData`

17. **`src/AIQ.Urban.Core/ModelActions/RemoveZoneStep.cs`** — Modify
    - Same pattern, using `ZoneDeletionData`

18. **`src/AIQ.Urban.Core/ModelActions/ResetUrbanElementsStep.cs`** — Modify
    - Change `: IDeleteUrbanElementsStep` → `: IUrbanModelStep`
    - No constructor params to remove (already parameterless)
    - Return `new StepResult([], ids)` — all GUIDs are deletions

### Phase 4: Refactor edit steps

Remove constructor params, extract from creation data, change return type.

19. **`src/AIQ.Urban.Core/ModelActions/ChangeBuildingUseStep.cs`** — Modify
    - Remove `List<Guid> geometryIds, Use newUse` from primary constructor
    - Change `: IEditUrbanElementsStep` → `: IUrbanModelStep`
    - In `Run()`: extract from `creationData.OfType<BuildingUseChangeData>().First()`
    - Return `new StepResult(touchedIds, touchedIds)` (in-place edit)

20. **`src/AIQ.Urban.Core/ModelActions/ChangeParcelUseStep.cs`** — Modify
    - Same pattern, using `ParcelUseChangeData`

21. **`src/AIQ.Urban.Core/ModelActions/UpdateTransitStep.cs`** — Modify
    - Remove `Guid _oldCenterlineId, Curve _newCenterline` from constructor
    - In `Run()`: extract from `creationData.OfType<TransitUpdateData>().First()`
    - Return `new StepResult(newIds, oldIds)` (replace = add new + delete old)

22. **`src/AIQ.Urban.Core/ModelActions/UpdateZoneStep.cs`** — Modify
    - Same pattern, using `ZoneUpdateData`

23. **`src/AIQ.Urban.Core/ModelActions/UpdateBuildingStep.cs`** — Modify
    - Remove `Guid _oldEnvelopeId, Brep _newEnvelope, double _typicalFloorHeight, List<Use> _oldPlateUses` from constructor
    - In `Run()`: extract from `creationData.OfType<BuildingUpdateData>().First()`
    - Return `new StepResult(newIds, oldIds)`

24. **`src/AIQ.Urban.Core/ModelActions/ReplaceFloorPlateStep.cs`** — Modify
    - Remove `FloorPlate oldPlate, double newZ` from constructor
    - In `Run()`: extract from `creationData.OfType<FloorPlateReplaceData>().First()`
    - Return `new StepResult(newIds, oldIds)`

25. **`src/AIQ.Urban.Core/ModelActions/ModifyModelUsesStep.cs`** — Modify
    - Remove `UseEditType editType, string name, Color color, Guid id` from constructor
    - In `Run()`: extract from `creationData.OfType<UseModificationData>().First()`
    - Drop `AffectedUse` property
    - Return `new StepResult(geometryIds, geometryIds)` (in-place edit)

### Phase 5: Refactor utility steps (return type only)

26. **`src/AIQ.Urban.Core/ModelActions/TransitSurfaceUnioner.cs`** — Modify
    - Change `: IEditUrbanElementsStep` → `: IUrbanModelStep`
    - Change `EditStepResult` → `StepResult`
    - Keep `_tolerance` in constructor (configuration, not data)

27. **`src/AIQ.Urban.Core/ModelActions/RecalculateParcelsStep.cs`** — Modify
    - Change `: IEditUrbanElementsStep` → `: IUrbanModelStep`
    - Change `EditStepResult` → `StepResult`
    - Already reads `CentroidUseData` from creation data — no other changes needed

28. **`src/AIQ.Urban.Core/ModelActions/GraphCreationStep.cs`** — Modify
    - Change `: IEditUrbanElementsStep` → `: IUrbanModelStep`
    - Change `EditStepResult` → `StepResult`
    - Keep `_approachTolerance, _tolerance` in constructor

### Phase 6: Update pipeline

29. **`src/AIQ.Urban.RhinoInterface/Pipeline/RhinoUrbanModelPipeline.cs`** — Modify
    - Remove `_editSteps` field and `List<IEditUrbanElementsStep>` type
    - Rename `_addDeleteSteps` → `_steps` (type `List<IUrbanModelStep>`)
    - Remove second `WithUrbanElementStep(IEditUrbanElementsStep)` overload — single `WithUrbanElementStep(IUrbanModelStep)` remains
    - Change `WithCreationData` from overwrite to accumulate: `_creationData.AddRange(data)`
    - In `Run()`: single loop over `_steps`, read `result.AddedIds` and `result.DeletedIds` directly — no marker interface checks

### Phase 7: Update PipelineConfigurations

30. **`src/AIQ.Urban.RhinoInterface/Pipeline/PipelineConfigurations.cs`** — Modify
    - All step constructors lose their data params (become parameterless or config-only)
    - Composition methods wrap data in appropriate creation data records
    - Chain `WithCreationData(...)` calls for each data type needed

    Specific methods:

    - `WithBuildingAdditionSteps` — no change (already passes `List<ICreationData>`)
    - `WithTransitAdditionSteps` — add `.WithCreationData([new TransitDeletionData(centerlinesToRemove)])` alongside existing transit data
    - `WithBuildingUseChangeSteps` — wrap `(geometryIds, newUse)` in `BuildingUseChangeData`
    - `WithZoneAdditionSteps` — add `.WithCreationData([new ZoneDeletionData(idsToDelete)])` alongside zone data
    - `WithTransitDeletionSteps` — wrap `centerlineIds` in `TransitDeletionData`
    - `WithTransitUpdateSteps` — wrap `(oldCenterlineId, newCenterline)` in `TransitUpdateData`
    - `WithBuildingDeletionSteps` — wrap `geometryIds` in `BuildingDeletionData`
    - `WithZoneDeletionSteps` — wrap `geometryIds` in `ZoneDeletionData`
    - `WithZoneUpdateSteps` — wrap `(oldBoundaryId, newBoundary)` in `ZoneUpdateData`
    - `WithBuildingUpdateSteps` — wrap all params in `BuildingUpdateData`
    - `WithFloorPlateReplaceSteps` — wrap `(oldPlate, newZ)` in `FloorPlateReplaceData`
    - `WithLanduseListEditSteps` — wrap params in `UseModificationData`
    - `WithDocModelLoad` — no change (uses `WithDataGatherStep`, no explicit creation data)

### Phase 8: Update tests

All test files that reference `EditStepResult`, `IEditUrbanElementsStep`, `IAddUrbanElementsStep`, `IDeleteUrbanElementsStep`, or construct steps with data params must be updated.

**Core tests (`tests/AIQ.Urban.Core.Tests/ModelActions/`):**

31. `AddBuildingStepTests.cs` — Update return type assertions from `List<Guid>` to `StepResult.AddedIds`
32. `RemoveBuildingStepTests.cs` — Construct `RemoveBuildingStep()` (no args), pass `BuildingDeletionData` via creation data list
33. `AddTransitStepTests.cs` — Update return type assertions
34. `RemoveTransitStepTests.cs` — Same pattern as RemoveBuildingStepTests
35. `AddZoneStepTests.cs` — Update return type assertions
36. `RemoveZoneStepTests.cs` — Same pattern
37. `ChangeBuildingUseStepTests.cs` — Construct `ChangeBuildingUseStep()` (no args), pass `BuildingUseChangeData` via creation data
38. `ChangeParcelUseStepTests.cs` — Same pattern, using `ParcelUseChangeData`
39. `UpdateTransitStepTests.cs` — Construct with no args, pass `TransitUpdateData`
40. `UpdateZoneStepTests.cs` — Same pattern
41. `UpdateBuildingStepTests.cs` — Same pattern
42. `ReplaceFloorPlateStepTests.cs` — Same pattern
43. `ModifyModelUsesStepTests.cs` — Same pattern, using `UseModificationData`
44. `ResetUrbanElementsStepTests.cs` — Update return type assertions
45. `TransitSurfaceAndParcelCalculationTests.cs` — Update return type assertions
46. `GraphCreationStepTests.cs` — Update return type assertions (setup steps also change)

**RhinoPlugin tests (`tests/AIQ.Urban.RhinoPlugin.Tests/`):**

47. `ResetUrban_AIQUrbanTests.cs` — Update if directly constructing steps
48. `PipelineConfigurations/BuildingPipelineConfigurationTests.cs` — Verify composition methods still work
49. `PipelineConfigurations/TransitPipelineConfigurationTests.cs` — Same
50. `PipelineConfigurations/ZonePipelineConfigurationTests.cs` — Same
51. `HTML_UI/MessageHandlersTests.cs` — Verify message handlers still work (they call composition methods)

## Verification

After all changes:
1. `dotnet build ArchSitePlan.sln` — must compile cleanly
2. `dotnet test tests/AIQ.Urban.Core.Tests/` — all core step tests pass
3. `dotnet test tests/AIQ.Urban.RhinoPlugin.Tests/` — all pipeline tests pass
4. `dotnet test` — full test suite passes

## File Count

- 11 new creation data record files
- 1 interface file rewritten
- 17 step implementation files modified
- 1 pipeline file modified
- 1 configuration file modified
- ~21 test files modified
- **Total: ~52 files**
