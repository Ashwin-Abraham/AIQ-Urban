# Design: Migrate Urban Element Creation Data to `ICreationData` + `IRhinoDataGatherStep`

## Status

Approved — ready for implementation plan.

## Context

The `IRhinoSelectionStep` branch introduces two new abstractions:

- `ICreationData` — a marker interface for data that is consumed by add-steps to create urban elements.
- `IRhinoDataGatherStep` — a Rhino-doc-aware step that gathers user input/selection and returns `ICreationData`.

The goal is to move the construction of `TransitData`, `BuildingData`, `ZoneData`, etc. out of Rhino commands and HTML UI message handlers into self-contained gather steps, and feed that data into `IAddUrbanElementsStep` implementations through the pipeline.

## Goals

1. `TransitData`, `BuildingData`, and `ZoneData` all implement `ICreationData`.
2. `IRhinoDataGatherStep` is the single place that converts Rhino selections into creation data.
3. `IAddUrbanElementsStep` receives its creation data at execution time through the pipeline.
4. `IDeleteUrbanElementsStep` implementations remain unchanged in behavior (they do not consume the data).
5. The solution builds after the migration.

## Design Decisions

### 1. Base `IUrbanModelStep` signature receives `ICreationData`

Both `IAddUrbanElementsStep` and `IDeleteUrbanElementsStep` inherit from `IUrbanModelStep`. The pipeline stores both in a single ordered list. Therefore, the cleanest path is to change the base interface:

```csharp
public interface IUrbanModelStep
{
    public List<Guid> Run(UrbanModel model, ICreationData? creationData);
}
```

Add steps use the parameter; delete steps accept it and ignore it. `IEditUrbanElementsStep` is unchanged. The parameter is nullable so delete-only pipelines can run without any creation data.

### 2. `CreationDataCollection<T>` carries lists

`ICreationData` is a marker interface. Individual records (`BuildingData`, `TransitData`, `ZoneData`) implement it, but a pipeline run needs a collection. A single generic wrapper is used:

```csharp
public record CreationDataCollection<T>(List<T> Items) : ICreationData
    where T : ICreationData;
```

A gather step for transit returns `CreationDataCollection<TransitData>` (as `ICreationData`), and `AddTransitStep` casts the incoming data back to it.

### 3. Pipeline resolves creation data at `Run()` time

`RhinoUrbanModelPipeline` exposes two new configuration methods:

```csharp
public RhinoUrbanModelPipeline WithDataGatherStep(IRhinoDataGatherStep step)
public RhinoUrbanModelPipeline WithCreationData(ICreationData data)
```

`Run()` resolves the data source as follows:

1. If a data-gather step is configured, call it: `step.Run(_rhinoDoc)`.
2. Otherwise, use any directly provided `ICreationData`.
3. If neither is present and the pipeline contains an `IAddUrbanElementsStep`, throw `InvalidOperationException`.
4. Delete-only pipelines may run without any creation data.

### 4. Gather-step implementations are operation-specific

New internal classes in `AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions`:

| Step | Parameters | Returns |
|------|------------|---------|
| `SelectTransitCenterlinesStep` | `IEnumerable<RhinoObject> selectedObjects`, `double width`, `TransitType type` | `CreationDataCollection<TransitData>` |
| `SelectBuildingFootprintsStep` | `IEnumerable<RhinoObject> selectedObjects`, `double floorHeight`, `int floorCount`, `Use use`, `string generationType` | `CreationDataCollection<BuildingData>` |
| `SelectZoneBoundaryStep` | `IEnumerable<RhinoObject> selectedObjects`, `string name` | `CreationDataCollection<ZoneData>` |

The existing placeholder `SelectCurveStep` is removed/replaced by these typed steps.

### 5. Pipeline configurations remain backward-compatible

`PipelineConfigurations` keeps the same public signatures for tests and MCP callers:

```csharp
public static RhinoUrbanModelPipeline WithBuildingAdditionSteps(
    this RhinoUrbanModelPipeline pipeline,
    List<BuildingData> buildingData,
    List<Guid> curvesToDelete)
```

Internally each configuration wraps the provided list and calls `WithCreationData(new CreationDataCollection<T>(...))`.

Commands and HTML message handlers switch to `WithDataGatherStep(...)`. The gather step returns the creation data; the caller extracts the Rhino object IDs from the data records and passes them to the existing configuration overloads. The configuration receives an empty list for the data argument because the data will be supplied by the gather step at runtime:

```csharp
var rhinoObjs = centerlineRefs.Select(x => x.Object()).ToList();

var gatherStep = new SelectTransitCenterlinesStep(rhinoObjs, width, type);
var creationData = (CreationDataCollection<TransitData>)gatherStep.Run(doc);
var centerlineIds = creationData.Items.Select(t => t.Centerline.Id).ToList();

new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithDataGatherStep(gatherStep)
    .WithTransitAdditionSteps([], centerlineIds)
    .WithMetricsCalculation(...);
```

## File-by-File Impact

### Core (`AIQ.Urban.Core`)

- `ModelActions/CreationData/ICreationData.cs` — keep as marker interface.
- `ModelActions/CreationData/BuildingData.cs` — already implements `ICreationData`.
- `ModelActions/CreationData/TransitData.cs` — move record here and implement `ICreationData`.
- `ModelActions/CreationData/ZoneData.cs` — move record here and implement `ICreationData`.
- `ModelActions/CreationData/CreationDataCollection.cs` — new generic wrapper.
- `ModelActions/IUrbanModelStep.cs` — change `Run` signature.
- `ModelActions/AddBuildingStep.cs` — accept `ICreationData` in `Run`; cast to `CreationDataCollection<BuildingData>`.
- `ModelActions/AddTransitStep.cs` — remove `TransitData` record; accept `ICreationData` in `Run`.
- `ModelActions/AddZoneStep.cs` — remove `ZoneData` record; accept `ICreationData` in `Run`.
- `ModelActions/RemoveBuildingStep.cs` — add unused `ICreationData` parameter.
- `ModelActions/RemoveTransitStep.cs` — add unused `ICreationData` parameter.
- `ModelActions/RemoveZoneStep.cs` — add unused `ICreationData` parameter.
- `ModelActions/ResetUrbanElementsStep.cs` — add unused `ICreationData` parameter.

### Interface (`AIQ.Urban.RhinoInterface`)

- `Pipeline/RhinoDocActions/IRhinoDataGatherStep.cs` — keep signature.
- `Pipeline/RhinoDocActions/SelectCurveStep.cs` — remove or replace.
- `Pipeline/RhinoDocActions/SelectTransitCenterlinesStep.cs` — new.
- `Pipeline/RhinoDocActions/SelectBuildingFootprintsStep.cs` — new.
- `Pipeline/RhinoDocActions/SelectZoneBoundaryStep.cs` — new.
- `Pipeline/RhinoUrbanModelPipeline.cs` — add `WithDataGatherStep`/`WithCreationData`, resolve data, pass to steps.
- `Pipeline/PipelineConfigurations.cs` — wrap data with `CreationDataCollection<T>`; existing overloads remain unchanged.

### Plugin (`AIQ.Urban.RhinoPlugin`)

- `Commands/AddTransit_AIQUrban.cs` — use `SelectTransitCenterlinesStep`.
- `Commands/AddBuilding_AIQUrban.cs` — use `SelectBuildingFootprintsStep`.
- `Commands/AddZone_AIQUrban.cs` — use `SelectZoneBoundaryStep`.
- `HTML_UI/MessagesFromUI/TransitMessage.cs` — use `SelectTransitCenterlinesStep`.
- `HTML_UI/MessagesFromUI/BuildingMessage.cs` — use `SelectBuildingFootprintsStep`.
- `MCP_Bridge/UrbanModelTools.cs` — no change in behavior; configurations wrap data internally.

### Tests

- `AIQ.Urban.Core.Tests` — update `Run` calls to pass `CreationDataCollection<T>`.
- `AIQ.Urban.RhinoPlugin.Tests` — update pipeline tests where needed; `PipelineConfigurations` callers stay the same.

## Edge Cases

1. **Delete-only pipelines** can run without `ICreationData` because no add step needs it.
2. **Empty creation data** is valid; add steps iterate empty collections and return no GUIDs.
3. **Mixed add/delete step lists** (e.g., `WithTransitAdditionSteps` includes `RemoveTransitStep`) receive the same `ICreationData`; the delete step ignores it.
4. **Edit steps** (`TransitSurfaceUnioner`, `RecalculateParcelsStep`, `UpdateTransitStep`, `UpdateZoneStep`, `ChangeBuildingUseStep`) are `IEditUrbanElementsStep` and are not affected.
5. **MCP tools** do not need gather steps; they continue to call configurations with concrete data.
6. **Direct step unit tests** must pass the correct `CreationDataCollection<T>` because `IUrbanModelStep.Run` now requires it.

## Open Questions

None — all design decisions are resolved.
