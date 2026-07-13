# Modular Rhino Output Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Rhino output opt-in and configurable — replace hardcoded outputter list with a registrable pipeline of `IUrbanModelRhinoOutputter` instances, defaulting to no output.

**Architecture:** Change `IUrbanModelRhinoOutputter.Run` to accept filtered elements. Remove `_outputToRhino` flag and `WithoutModelOutput()`. Add `_outputters` list with `WithFullModelOutput()` and `WithRhinoOutputter<T>()` configuration methods. All 22 call sites updated to explicitly opt in.

**Tech Stack:** C# 12, .NET 8, RhinoCommon, xUnit

## Global Constraints

- C# 12 with file-scoped namespaces
- 4-space indentation, K&R braces
- No comments unless asked
- Nullable reference types enabled
- Target: `AIQ.Urban.RhinoInterface` project + callers across solution

---

## File Map

| File | Action | Purpose |
|------|--------|---------|
| `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/IUrbanModelRhinoOutputter.cs` | Modify | Add `List<IUrbanElement>` parameter to `Run` |
| `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/TransitOutputter.cs` | Modify | Parameterless constructor, accept elements in `Run` |
| `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/BuildingOutputter.cs` | Modify | Parameterless constructor, accept elements in `Run` |
| `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/ParcelOutputter.cs` | Modify | Parameterless constructor, accept elements in `Run` |
| `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/ZoneOutputter.cs` | Modify | Parameterless constructor, accept elements in `Run` |
| `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/TransitSurfaceOutputter.cs` | Modify | Accept elements in `Run` (ignore them) |
| `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoUrbanModelPipeline.cs` | Modify | Add `_outputters`, `WithFullModelOutput()`, `WithRhinoOutputter<T>()`, remove `_outputToRhino`/`WithoutModelOutput()` |
| `src/AIQ.Urban.RhinoInterface/Pipeline/PipelineConfigurations.cs` | Modify | Remove `.WithoutModelOutput()` from `WithDocModelLoad` |
| 5 Command files | Modify | Add `.WithFullModelOutput()` to pipeline chains |
| 5 UI Message files | Modify | Add `.WithFullModelOutput()` to pipeline chains |
| `EventHandler/RhinoUrbanEventHandler.cs` | Modify | Add `.WithFullModelOutput()` to 5 handler chains |
| `MCP_Bridge/RhinoPipelineExecutorForMcp.cs` + `UrbanModelTools.cs` | Modify | Add `.WithFullModelOutput()` to pipeline |
| 4 Test files | Modify | Add `.WithFullModelOutput()` to pipeline chains |

---

### Task 1: Change `IUrbanModelRhinoOutputter` interface

**Files:**
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/IUrbanModelRhinoOutputter.cs`

- [ ] **Step 1: Update interface signature**

Change `Run(UrbanModel model, RhinoDoc doc)` to `Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements)`:

```csharp
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public interface IUrbanModelRhinoOutputter
{
    void Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements);
}
```

- [ ] **Step 2: Build to confirm all implementations break**

Run: `dotnet build src/AIQ.Urban.RhinoInterface/AIQ.Urban.RhinoInterface.csproj 2>&1 | Select-String "error CS"`
Expected: 5 errors — each outputter's `Run` method no longer satisfies the interface.

---

### Task 2: Update all outputter implementations

**Files:**
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/TransitOutputter.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/BuildingOutputter.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/ParcelOutputter.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/ZoneOutputter.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/TransitSurfaceOutputter.cs`

- [ ] **Step 1: Update TransitOutputter**

Remove primary constructor parameter. Change `Run` to accept elements, filter to `List<Transit>`, and use that instead of the stored field:

```csharp
using System.Drawing;
using AIQ.Primitives.Extensions;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Utils;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class TransitOutputter : IUrbanModelRhinoOutputter
{
    private static readonly Dictionary<TransitType, (int Hue, double SatMin, double SatMax, double LightMin, double LightMax)> ColorConfig = new()
    {
        [TransitType.Road] = (0, 0, 0, 45, 85),
        [TransitType.Railway] = (25, 55, 95, 20, 45),
        [TransitType.CyclePath] = (120, 40, 85, 20, 50),
        [TransitType.Pedestrian] = (43, 55, 95, 30, 60),
        [TransitType.Waterway] = (219, 55, 95, 45, 75),
    };

    public void Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements)
    {
        var transits = elements.OfType<Transit>().ToList();
        SetTransitColors(transits);

        foreach (var transit in transits)
        {
            var transitColor = transit.Color ?? ColorUtils.GetTransitTypeColor(transit.TransitType);

            RhinoOutputHelper.OutputGeometry(doc, transit.Outline, RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.TransitOutline(transit.TransitType, transit.Width), transitColor), false);
            RhinoOutputHelper.OutputGeometry(doc, transit.Centerline, RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.TransitCenterline(transit.TransitType, transit.Width), transitColor));
        }
    }

    private static void SetTransitColors(List<Transit> transits)
    {
        var widthRanges = transits
            .GroupBy(t => t.TransitType)
            .ToDictionary(g => g.Key, g => (Min: g.Min(t => t.Width), Max: g.Max(t => t.Width)));

        foreach (var transit in transits)
        {
            if (!ColorConfig.TryGetValue(transit.TransitType, out var cfg))
                continue;

            var range = widthRanges.GetValueOrDefault(transit.TransitType);
            var t = range.Max == range.Min ? 0.5
                : Math.Clamp((transit.Width - range.Min) / (range.Max - range.Min), 0, 1);

            var s = cfg.SatMin + t * (cfg.SatMax - cfg.SatMin);
            var l = cfg.LightMax - t * (cfg.LightMax - cfg.LightMin);

            transit.Color = ColorExtensions.FromHsl(cfg.Hue, s, l);
        }
    }
}
```

- [ ] **Step 2: Update BuildingOutputter**

```csharp
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Utils;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class BuildingOutputter : IUrbanModelRhinoOutputter
{
    public void Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements)
    {
        var buildings = elements.OfType<Building>().ToList();
        var envelopeAttributes = RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.BuildingEnvelope, ColorUtils.BuildingEnvelopeColor);

        foreach (var building in buildings)
        {
            var newEnvelopeId = doc.Objects.AddBrep(building.Envelope.Geometry, envelopeAttributes);
            building.Envelope.Id = newEnvelopeId;

            foreach (var plate in building.FloorPlates)
            {
                var plateAttributes = RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.BuildingFloorPlate(plate.Use), plate.Use.Color);
                var newId = doc.Objects.AddCurve(plate.Outline.Geometry, plateAttributes);
                plate.Outline.Id = newId;
            }
        }
    }
}
```

- [ ] **Step 3: Update ParcelOutputter**

```csharp
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class ParcelOutputter : IUrbanModelRhinoOutputter
{
    public void Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements)
    {
        var parcels = elements.OfType<Parcel>().ToList();
        foreach (var parcel in parcels)
            RhinoOutputHelper.OutputGeometry(
                doc,
                parcel.Outline,
                RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.ParcelLayer(parcel.Use), parcel.Use.Color),
                true);
    }
}
```

- [ ] **Step 4: Update ZoneOutputter**

```csharp
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Utils;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class ZoneOutputter : IUrbanModelRhinoOutputter
{
    public void Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements)
    {
        var zones = elements.OfType<Zone>().ToList();
        foreach (var zone in zones)
            RhinoOutputHelper.OutputGeometry(
                doc,
                zone.Boundary,
                RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.ZoneLayer(zone.Name), ColorUtils.DefaultZoneColor),
                false);
    }
}
```

- [ ] **Step 5: Update TransitSurfaceOutputter**

Accept elements parameter but ignore it (reads from `model.TransitSurfaces`):

```csharp
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Utils;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class TransitSurfaceOutputter : IUrbanModelRhinoOutputter
{
    public void Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements)
    {
        foreach (var (profile, type) in model.TransitSurfaces)
            RhinoOutputHelper.OutputGeometry(doc, profile, RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.TransitSurface(type), ColorUtils.GetTransitTypeColor(type)), true);
    }
}
```

- [ ] **Step 6: Build to verify outputters compile**

Run: `dotnet build src/AIQ.Urban.RhinoInterface/AIQ.Urban.RhinoInterface.csproj 2>&1 | Select-String "error CS"`
Expected: Outputter files compile. Remaining errors are in `RhinoUrbanModelPipeline.cs` (the old `AddRhinoObjects` call site).

---

### Task 3: Refactor pipeline — add `_outputters` list, `WithFullModelOutput()`, `WithRhinoOutputter<T>()`

**Files:**
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoUrbanModelPipeline.cs`

- [ ] **Step 1: Full file rewrite**

Remove `_outputToRhino` field (line 18) and `WithoutModelOutput()` method (lines 60-64). Add `_outputters` list, `WithFullModelOutput()`, and `WithRhinoOutputter<T>()`. Update `Run()` to check `_outputters.Count > 0` instead of `_outputToRhino`. Update `AddRhinoObjects` to iterate `_outputters` and pass `elementsToAdd` to each:

```csharp
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.EventHandler;
using AIQ.Urban.RhinoInterface.Extensions;
using AIQ.Urban.RhinoInterface.Pipeline.Outputters;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;

namespace AIQ.Urban.RhinoInterface;

public class RhinoUrbanModelPipeline(
    RhinoDoc _rhinoDoc,
    UrbanModel _urbanModel)
{
    private IRhinoDataGatherStep? _objectSelectionStep;
    private List<ICreationData> _creationData = [];
    private readonly List<IUrbanModelStep> _addDeleteSteps = [];
    private readonly List<IEditUrbanElementsStep> _editSteps = [];
    private readonly List<Guid> _directDeleteIds = [];
    private readonly List<Action<UrbanModel>> _postActions = [];
    private readonly List<IUrbanModelRhinoOutputter> _outputters = [];

    private RhinoUrbanEventHandler? _eventHandler = null;

    internal UrbanModel UrbanModel => _urbanModel;

    public RhinoUrbanModelPipeline WithEventHandler(RhinoUrbanEventHandler eventHandler)
    {
        _eventHandler = eventHandler;
        return this;
    }

    public RhinoUrbanModelPipeline WithDataGatherStep(IRhinoDataGatherStep step)
    {
        _objectSelectionStep = step;
        return this;
    }

    public RhinoUrbanModelPipeline WithCreationData(List<ICreationData> data)
    {
        _creationData = data;
        return this;
    }

    public RhinoUrbanModelPipeline WithRhinoObjectDeletion(List<Guid> ids)
    {
        _directDeleteIds.AddRange(ids);
        return this;
    }

    public RhinoUrbanModelPipeline WithUrbanElementStep(IUrbanModelStep step)
    {
        _addDeleteSteps.Add(step);
        return this;
    }

    public RhinoUrbanModelPipeline WithUrbanElementStep(IEditUrbanElementsStep step)
    {
        _editSteps.Add(step);
        return this;
    }

    public RhinoUrbanModelPipeline WithFullModelOutput()
    {
        _outputters.Add(new TransitOutputter());
        _outputters.Add(new BuildingOutputter());
        _outputters.Add(new ParcelOutputter());
        _outputters.Add(new ZoneOutputter());
        _outputters.Add(new TransitSurfaceOutputter());
        return this;
    }

    public RhinoUrbanModelPipeline WithRhinoOutputter<T>() where T : IUrbanModelRhinoOutputter, new()
    {
        _outputters.Add(new T());
        return this;
    }

    /// <summary>Register an action to execute after all steps, deletions, and baking complete.</summary>
    public RhinoUrbanModelPipeline WithUrbanModelPostAction(Action<UrbanModel> action)
    {
        _postActions.Add(action);
        return this;
    }

    public void Run()
    {
        if (_eventHandler is not null)
        {
            _eventHandler.EventLock = true;
        }

        var deletedIds = new List<Guid>();
        var addedIds = new List<Guid>();

        var creationData = _objectSelectionStep?.Run(_rhinoDoc) ?? _creationData;

        foreach (var step in _addDeleteSteps)
        {
            var result = step.Run(_urbanModel, creationData);

            if (step is IDeleteUrbanElementsStep)
                deletedIds.AddRange(result);
            if (step is IAddUrbanElementsStep)
                addedIds.AddRange(result);
        }

        foreach (var step in _editSteps)
        {
            var result = step.Run(_urbanModel, creationData);
            addedIds.AddRange(result.AddedIds);
            deletedIds.AddRange(result.DeletedIds);
        }

        deletedIds.AddRange(_directDeleteIds);

        if(_outputters.Count > 0)
        {
            DeleteRhinoObjects(deletedIds);
            AddRhinoObjects(addedIds);
            _rhinoDoc.Layers.PurgeEmpty(_rhinoDoc, LayerConstants.Root);
        }

        foreach (var action in _postActions)
            action(_urbanModel);

        if (_eventHandler is not null)
        {
            _eventHandler.RebuildLookup();
            _eventHandler.EventLock = false;
        }
    }

    private void AddRhinoObjects(List<Guid> addedIds)
    {
        var elementsToAdd = FilterElementsToAdd(addedIds);

        foreach (var outputter in _outputters)
            outputter.Run(_urbanModel, _rhinoDoc, elementsToAdd);
    }

    private List<IUrbanElement> FilterElementsToAdd(List<Guid> addedIds)
    {
        return _urbanModel.GetUrbanElements()
            .Where(elem => elem.GetGeometryIds().Any(elemtnId => addedIds.Contains(elemtnId)))
            .ToList();
    }

    private void DeleteRhinoObjects(List<Guid> idsToDelete)
    {
        var deletableObjects = _rhinoDoc.Objects.Where(o => idsToDelete.Contains(o.Id)).ToList();
        deletableObjects.ForEach(obj => _rhinoDoc.Objects.Delete(obj, true, true));
    }
}
```

- [ ] **Step 2: Build to confirm callers break**

Run: `dotnet build ArchSitePlan.sln 2>&1 | Select-String "error CS"`
Expected: Errors in `PipelineConfigurations.cs` (`.WithoutModelOutput()` gone).

---

### Task 4: Update PipelineConfigurations — remove `.WithoutModelOutput()`

**Files:**
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/PipelineConfigurations.cs:188`

- [ ] **Step 1: Remove `.WithoutModelOutput()` from `WithDocModelLoad`**

In `WithDocModelLoad()` (line 173), remove `.WithoutModelOutput()` from the chain. Output is now off by default, so this method correctly produces no output:

```csharp
    public static RhinoUrbanModelPipeline WithDocModelLoad(
        this RhinoUrbanModelPipeline pipeline,
        RhinoDoc doc)
    {
        var model = pipeline.UrbanModel;

        var gatherStep = new DocLoadGatherStep(model, doc.ModelAbsoluteTolerance);

        pipeline.WithDataGatherStep(gatherStep)
            .WithUrbanElementStep(new ResetUrbanElementsStep())
            .WithUrbanElementStep(new AddTransitStep())
            .WithUrbanElementStep(new AddBuildingStep())
            .WithUrbanElementStep(new AddZoneStep())
            .WithUrbanElementStep(new TransitSurfaceUnioner())
            .WithUrbanElementStep(new RecalculateParcelsStep())
            .WithMetricsCalculation();

        return pipeline;
    }
```

- [ ] **Step 2: Build the RhinoInterface project**

Run: `dotnet build src/AIQ.Urban.RhinoInterface/AIQ.Urban.RhinoInterface.csproj`
Expected: Clean build.

---

### Task 5: Add `.WithFullModelOutput()` to all command and UI callers

**Files:**
- Modify: `src/AIQ.Urban.Rhino/Commands/AddTransit_AIQUrban.cs`
- Modify: `src/AIQ.Urban.Rhino/Commands/AddBuilding_AIQUrban.cs`
- Modify: `src/AIQ.Urban.Rhino/Commands/AddZone_AIQUrban.cs`
- Modify: `src/AIQ.Urban.Rhino/Commands/ChangeBuildingUse_AIQUrban.cs`
- Modify: `src/AIQ.Urban.Rhino/Commands/ResetUrban_AIQUrban.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/TransitMessage.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/BuildingMessage.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/AddZoneMessage.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/ChangeUseMessage.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/LanduseListEditMessage.cs`

For each file, add `.WithFullModelOutput()` to the pipeline chain. Place it after the last `.With*Step()` or `.WithCreationData()` call and before `.WithMetricsCalculation()` or `.WithEventHandler()`.

- [ ] **Step 1: AddTransit_AIQUrban.cs**

```csharp
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithDataGatherStep(gatherStep)
    .WithTransitAdditionSteps([], centerlineIds)
    .WithFullModelOutput()
    .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name)
    .WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);
```

- [ ] **Step 2: AddBuilding_AIQUrban.cs**

```csharp
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithDataGatherStep(gatherStep)
    .WithBuildingAdditionSteps([], idsToDelete)
    .WithFullModelOutput()
    .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name)
    .WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);
```

- [ ] **Step 3: AddZone_AIQUrban.cs**

```csharp
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithDataGatherStep(gatherStep)
    .WithZoneAdditionSteps([], idsToDelete)
    .WithFullModelOutput()
    .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name)
    .WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);
```

- [ ] **Step 4: ChangeBuildingUse_AIQUrban.cs**

```csharp
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithBuildingUseChangeSteps(geometryIds, selectedUse)
    .WithFullModelOutput()
    .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name)
    .WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);
```

- [ ] **Step 5: ResetUrban_AIQUrban.cs**

```csharp
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
pipeline.WithUrbanElementStep(new ResetUrbanElementsStep());
pipeline.WithFullModelOutput();
pipeline.WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);
pipeline.Run();
```

- [ ] **Step 6: TransitMessage.cs**

```csharp
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithDataGatherStep(gatherStep)
    .WithTransitAdditionSteps([], centerlineIds)
    .WithFullModelOutput()
    .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);
```

- [ ] **Step 7: BuildingMessage.cs**

```csharp
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithDataGatherStep(gatherStep)
    .WithBuildingAdditionSteps([], idsToDelete)
    .WithFullModelOutput()
    .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);
```

- [ ] **Step 8: AddZoneMessage.cs**

```csharp
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithDataGatherStep(gatherStep)
    .WithZoneAdditionSteps([], idsToDelete)
    .WithFullModelOutput()
    .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);
```

- [ ] **Step 9: ChangeUseMessage.cs**

```csharp
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithUrbanElementStep(parcelStep)
    .WithUrbanElementStep(buildingStep)
    .WithFullModelOutput()
    .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);
```

- [ ] **Step 10: LanduseListEditMessage.cs**

```csharp
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithLanduseListEditSteps(editType, name, parsedColor, guid)
    .WithFullModelOutput()
    .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);
```

- [ ] **Step 11: Build the Rhino project**

Run: `dotnet build src/AIQ.Urban.Rhino/AIQ.Urban.Rhino.csproj`
Expected: Clean build.

---

### Task 6: Add `.WithFullModelOutput()` to event handler and MCP bridge

**Files:**
- Modify: `src/AIQ.Urban.RhinoInterface/EventHandler/RhinoUrbanEventHandler.cs`
- Inspect: `src/AIQ.Urban.Rhino/MCP_Bridge/UrbanModelTools.cs` (verify callback chains)

- [ ] **Step 1: Update RhinoUrbanEventHandler.cs — 5 handler methods**

In each handler, add `.WithFullModelOutput()` to the pipeline chain before `.WithMetricsCalculation()`:

**HandleTransitDelete (line ~227):**
```csharp
var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance)
    .WithTransitDeletionSteps(centerlineIds)
    .WithFullModelOutput()
    .WithMetricsCalculation();
```

**HandleTransitReplace (line ~245):**
```csharp
var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance)
    .WithTransitUpdateSteps(oldCenterlineId, newCurve)
    .WithFullModelOutput()
    .WithMetricsCalculation();
```

**HandleBuildingDelete (line ~256):**
```csharp
var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance)
    .WithBuildingDeletionSteps(geometryIds)
    .WithFullModelOutput()
    .WithMetricsCalculation();
```

**HandleZoneDelete (line ~267):**
```csharp
var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance)
    .WithZoneDeletionSteps(geometryIds)
    .WithFullModelOutput()
    .WithMetricsCalculation();
```

**HandleZoneReplace (line ~285):**
```csharp
var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance)
    .WithZoneUpdateSteps(oldBoundaryId, newCurve)
    .WithFullModelOutput()
    .WithMetricsCalculation();
```

- [ ] **Step 2: Verify MCP bridge callers**

Read `src/AIQ.Urban.Rhino/MCP_Bridge/UrbanModelTools.cs`. If pipeline chains are built inside the `Action<RhinoUrbanModelPipeline>` callbacks, add `.WithFullModelOutput()` to each callback. If `RhinoPipelineExecutorForMcp.cs` builds the pipeline itself, add it there instead.

- [ ] **Step 3: Build the full solution**

Run: `dotnet build ArchSitePlan.sln`
Expected: Clean build across all projects.

---

### Task 7: Update test files

**Files:**
- Modify: `tests/AIQ.Urban.RhinoPlugin.Tests/ResetUrban_AIQUrbanTests.cs`
- Modify: `tests/AIQ.Urban.RhinoPlugin.Tests/PipelineConfigurations/BuildingPipelineConfigurationTests.cs`
- Modify: `tests/AIQ.Urban.RhinoPlugin.Tests/PipelineConfigurations/ZonePipelineConfigurationTests.cs`
- Modify: `tests/AIQ.Urban.RhinoPlugin.Tests/PipelineConfigurations/TransitPipelineConfigurationTests.cs`

- [ ] **Step 1: Update ResetUrban_AIQUrbanTests.cs — 2 pipeline constructions**

First pipeline (line ~37):
```csharp
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithBuildingAdditionSteps(new System.Collections.Generic.List<ICreationData> { buildingData }, [])
    .WithFullModelOutput()
    .WithMetricsCalculation(false);
```

Second pipeline (line ~49):
```csharp
pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
pipeline.WithUrbanElementStep(new ResetUrbanElementsStep());
pipeline.WithFullModelOutput();
pipeline.Run();
```

- [ ] **Step 2: Update BuildingPipelineConfigurationTests.cs**

```csharp
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
pipeline
    .WithBuildingAdditionSteps(testData.Input.Cast<ICreationData>().ToList(), [])
    .WithFullModelOutput()
    .WithMetricsCalculation(false)
    .Run();
```

- [ ] **Step 3: Update ZonePipelineConfigurationTests.cs**

```csharp
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithZoneAdditionSteps(testData.Input, [])
    .WithFullModelOutput()
    .WithMetricsCalculation(false);
```

- [ ] **Step 4: Update TransitPipelineConfigurationTests.cs**

```csharp
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
pipeline
    .WithTransitAdditionSteps(testData.Input, testData.CenterlineIds)
    .WithFullModelOutput()
    .WithMetricsCalculation(false)
    .Run();
```

- [ ] **Step 5: Build and run tests**

Run: `dotnet build ArchSitePlan.sln`
Run: `dotnet test`
Expected: All tests pass.

---

### Task 8: Verify and commit

- [ ] **Step 1: Full build**

Run: `dotnet build ArchSitePlan.sln -c Debug`
Expected: Clean build, no warnings related to outputters.

- [ ] **Step 2: Run all tests**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 3: Verify no references to removed API**

Run: `rg "WithoutModelOutput|_outputToRhino"` across the repo.
Expected: Zero matches.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor: make Rhino output opt-in via WithFullModelOutput() pipeline configuration

- Change IUrbanModelRhinoOutputter.Run to accept List<IUrbanElement> elements
- Make outputter constructors parameterless
- Add _outputters list with WithFullModelOutput() and WithRhinoOutputter<T>()
- Remove WithoutModelOutput() and _outputToRhino flag
- Output is now off by default (opt-in required)
- Update all 22 call sites to explicitly register outputters"
```
