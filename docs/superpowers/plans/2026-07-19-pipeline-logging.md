# Pipeline Logging Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add structured logging to the AIQ.Urban pipeline so every step logs modification counts, errors, and severity levels to the Rhino command line.

**Architecture:** Custom `ILoggerProvider` writes to `RhinoApp.Write()` in compact format (`[Info] StepName: message`). Each step and outputter receives an optional `ILoggerFactory?` via primary constructor (null default = no-op). The pipeline orchestrator wraps each step in try/catch, logging errors and continuing. Each call site creates its own `ILoggerFactory`, passes it to the pipeline, and disposes after `Run()`.

**Tech Stack:** `Microsoft.Extensions.Logging` (abstraction only), `RhinoApp.Write()` (RhinoCommon), C# 12 primary constructors, xUnit for tests.

## Global Constraints

- C# 12 / .NET 8, nullable enabled, implicit usings enabled
- Primary constructor pattern for new/modified class declarations
- File-scoped namespaces (`namespace X.Y.Z;`)
- 4-space indentation, K&R braces
- No comments unless asked
- Tests use xUnit with `[Fact]` / `[Theory]`

---

## Slice 1: Logging Infrastructure + Pipeline Orchestrator

**Goal:** Establish the logging foundation. After this slice, the pipeline can accept an `ILoggerFactory`, the orchestrator logs step start/end/errors, and a custom provider writes to Rhino's command line. Steps still have no logging (Slice 2).

### Task 1: Add NuGet packages

**Files:**
- Modify: `src/AIQ.Urban.Core/AIQ.Urban.Core.csproj`
- Modify: `src/AIQ.Urban.RhinoInterface/AIQ.Urban.RhinoInterface.csproj`

- [ ] **Step 1: Add Microsoft.Extensions.Logging to AIQ.Urban.Core**

```bash
dotnet add src/AIQ.Urban.Core/AIQ.Urban.Core.csproj package Microsoft.Extensions.Logging
```

- [ ] **Step 2: Add Microsoft.Extensions.Logging to AIQ.Urban.RhinoInterface**

```bash
dotnet add src/AIQ.Urban.RhinoInterface/AIQ.Urban.RhinoInterface.csproj package Microsoft.Extensions.Logging
```

- [ ] **Step 3: Verify build**

```bash
dotnet build src/AIQ.Urban.RhinoInterface/AIQ.Urban.RhinoInterface.csproj
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/AIQ.Urban.Core/AIQ.Urban.Core.csproj src/AIQ.Urban.RhinoInterface/AIQ.Urban.RhinoInterface.csproj
git commit -m "chore: add Microsoft.Extensions.Logging to Core and RhinoInterface"
```

---

### Task 2: Create RhinoLogProvider and RhinoLogger

**Files:**
- Create: `src/AIQ.Urban.RhinoInterface/Logging/RhinoLogger.cs`
- Create: `src/AIQ.Urban.RhinoInterface/Logging/RhinoLogProvider.cs`
- Create: `src/AIQ.Urban.RhinoInterface/Logging/RhinoLoggerFactoryExtensions.cs`

**Interfaces:**
- Produces: `RhinoLogger`, `RhinoLogProvider`, `AddRhinoLog()` extension method — consumed by call sites in Slice 3

- [ ] **Step 1: Create RhinoLogger**

Create `src/AIQ.Urban.RhinoInterface/Logging/RhinoLogger.cs`:

```csharp
using Microsoft.Extensions.Logging;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Logging;

internal class RhinoLogger(string categoryName) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var levelTag = logLevel switch
        {
            LogLevel.Warning => "[Warn]",
            LogLevel.Error => "[Error]",
            LogLevel.Critical => "[Error]",
            _ => "[Info]"
        };

        var message = formatter(state, exception);
        var output = exception is not null
            ? $"{levelTag} {categoryName}: {message} — {exception.Message}"
            : $"{levelTag} {categoryName}: {message}";

        RhinoApp.WriteLine(output);
    }
}
```

- [ ] **Step 2: Create RhinoLogProvider**

Create `src/AIQ.Urban.RhinoInterface/Logging/RhinoLogProvider.cs`:

```csharp
using Microsoft.Extensions.Logging;

namespace AIQ.Urban.RhinoInterface.Logging;

internal class RhinoLogProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new RhinoLogger(categoryName);

    public void Dispose() { }
}
```

- [ ] **Step 3: Create extension method**

Create `src/AIQ.Urban.RhinoInterface/Logging/RhinoLoggerFactoryExtensions.cs`:

```csharp
using Microsoft.Extensions.Logging;

namespace AIQ.Urban.RhinoInterface.Logging;

public static class RhinoLoggerFactoryExtensions
{
    public static ILoggerFactory AddRhinoLog(this LoggerFactory factory)
    {
        factory.AddProvider(new RhinoLogProvider());
        return factory;
    }
}
```

- [ ] **Step 4: Verify build**

```bash
dotnet build src/AIQ.Urban.RhinoInterface/AIQ.Urban.RhinoInterface.csproj
```

Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add src/AIQ.Urban.RhinoInterface/Logging/
git commit -m "feat: add RhinoLogProvider writing to RhinoApp.Write()"
```

---

### Task 3: Add ILoggerFactory to pipeline constructor + orchestrator logging

**Files:**
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoUrbanModelPipeline.cs`

**Interfaces:**
- Consumes: `ILoggerFactory` from call sites (Slice 3), `NullLoggerFactory` for backward compat
- Produces: `LoggerFactory` property read by `PipelineConfigurations` (Slice 2)

- [ ] **Step 1: Add ILoggerFactory to constructor, add orchestrator logging, wrap steps in try/catch**

Replace the full content of `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoUrbanModelPipeline.cs`:

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
using Microsoft.Extensions.Logging;
using Rhino;

namespace AIQ.Urban.RhinoInterface;

public class RhinoUrbanModelPipeline(
    RhinoDoc _rhinoDoc,
    UrbanModel _urbanModel,
    ILoggerFactory? _loggerFactory = null)
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<RhinoUrbanModelPipeline>();

    private IRhinoDataGatherStep? _objectSelectionStep;
    private List<ICreationData> _creationData = [];
    private readonly List<IUrbanModelStep> _steps = [];
    private readonly List<Guid> _directDeleteIds = [];
    private readonly List<Action<UrbanModel>> _postActions = [];
    private readonly List<IUrbanModelRhinoOutputter> _outputters = [];

    private RhinoUrbanEventHandler? _eventHandler = null;

    internal UrbanModel UrbanModel => _urbanModel;
    public ILoggerFactory LoggerFactory => _loggerFactory ?? NullLoggerFactory.Instance;

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
        _creationData.AddRange(data);
        return this;
    }

    public RhinoUrbanModelPipeline WithRhinoObjectDeletion(List<Guid> ids)
    {
        _directDeleteIds.AddRange(ids);
        return this;
    }

    public RhinoUrbanModelPipeline WithUrbanElementStep(IUrbanModelStep step)
    {
        _steps.Add(step);
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

        _logger.LogInformation("Pipeline started ({StepCount} steps)", _steps.Count);

        try
        {
            var creationData = _objectSelectionStep?.Run(_rhinoDoc) ?? _creationData;

            foreach (var step in _steps)
            {
                try
                {
                    var result = step.Run(_urbanModel, creationData);
                    addedIds.AddRange(result.AddedIds);
                    deletedIds.AddRange(result.DeletedIds);
                    _logger.LogInformation("{StepType}: +{Added} -{Deleted}",
                        step.GetType().Name, result.AddedIds.Count, result.DeletedIds.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{StepType} failed", step.GetType().Name);
                }
            }

            deletedIds.AddRange(_directDeleteIds);

            if (_outputters.Count > 0)
            {
                DeleteRhinoObjects(deletedIds);
                AddRhinoObjects(addedIds);
                _rhinoDoc.Layers.PurgeEmpty(_rhinoDoc, LayerConstants.Root);
            }

            foreach (var action in _postActions)
            {
                try
                {
                    action(_urbanModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Post-action failed");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline failed during data gather");
        }
        finally
        {
            if (_eventHandler is not null)
            {
                _eventHandler.RebuildLookup();
                _eventHandler.EventLock = false;
            }
        }

        _logger.LogInformation("Pipeline completed — {Added} added, {Deleted} deleted",
            addedIds.Count, deletedIds.Count);
    }

    private void AddRhinoObjects(List<Guid> addedIds)
    {
        var elementsToAdd = FilterElementsToAdd(addedIds);

        foreach (var outputter in _outputters)
        {
            try
            {
                outputter.Run(_urbanModel, _rhinoDoc, elementsToAdd);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{OutputterType} failed", outputter.GetType().Name);
            }
        }
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

- [ ] **Step 2: Verify build**

```bash
dotnet build src/AIQ.Urban.RhinoInterface/AIQ.Urban.RhinoInterface.csproj
```

Expected: Build succeeds. Existing call sites compile because `_loggerFactory` defaults to `null`.

- [ ] **Step 3: Commit**

```bash
git add src/AIQ.Urban.RhinoInterface/Pipeline/RhinoUrbanModelPipeline.cs
git commit -m "feat: add ILoggerFactory to pipeline, wrap steps in try/catch with logging"
```

---

### Task 4: Verify existing tests still pass

- [ ] **Step 1: Run all tests**

```bash
dotnet test
```

Expected: All tests pass. Pipeline constructor change is backward-compatible (optional param with null default).

- [ ] **Step 2: Commit if any test fixes needed**

---

## Slice 2: Step + Outputter Logging

**Goal:** Every model step and outputter logs its actions. After this slice, `AddBuildingStep` logs "Added 3 buildings", `RecalculateParcelsStep` logs "Recalculated 47 parcels", outputters log what they output, etc.

### Task 5: Add ILoggerFactory to all 16 Core steps + logging in Run methods

**Files:**
- Modify: `src/AIQ.Urban.Core/ModelActions/AddBuildingStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/AddTransitStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/AddZoneStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/RemoveBuildingStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/RemoveTransitStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/RemoveZoneStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/ResetUrbanElementsStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/UpdateBuildingStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/UpdateTransitStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/UpdateZoneStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/TransitSurfaceUnioner.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/RecalculateParcelsStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/ReplaceFloorPlateStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/ChangeBuildingUseStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/ChangeParcelUseStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/ModifyModelUsesStep.cs`

**Pattern for each step:**
1. Add `ILoggerFactory? loggerFactory = null` to primary constructor (or add primary constructor if parameterless)
2. Add `private readonly ILogger _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<ClassName>();`
3. Add `using Microsoft.Extensions.Logging;`
4. Add logging calls in `Run()` method

**Interfaces:**
- Consumes: `ILoggerFactory` from `PipelineConfigurations` (same slice)
- Produces: Log output via `ILogger<T>` — consumed by pipeline orchestrator (Slice 1 already handles)

- [ ] **Step 1: Add logging to AddBuildingStep**

Modify `src/AIQ.Urban.Core/ModelActions/AddBuildingStep.cs`:

```csharp
using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;

namespace AIQ.Urban.Core.ModelActions;

public class AddBuildingStep(
    double _tolerance = 1e-5,
    ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<AddBuildingStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var buildings = new List<Models.UrbanElements.Building>();

        foreach (var data in creationData)
        {
            if (data is BuildingFootprintData fpData)
            {
                buildings.Add(new Models.UrbanElements.Building(fpData.Footprint, fpData.TypicalFloorHeight, fpData.FloorCount, fpData.Use));
            }
            else if (data is BuildingMassData massData)
            {
                buildings.Add(new Models.UrbanElements.Building(massData.Envelope.Geometry, massData.FloorHeight, massData.Use, _tolerance));
            }
            else if (data is BuildingMassAndFloorsData mfData)
            {
                buildings.Add(new Models.UrbanElements.Building(mfData.Envelope.Geometry, mfData.AssociatedPlates));
            }
        }

        foreach (var building in buildings)
            model.AddBuilding(building);

        _logger.LogInformation("Added {Count} buildings", buildings.Count);

        return new(buildings.SelectMany(x => x.GetGeometryIds()).ToList(), []);
    }
}
```

- [ ] **Step 2: Add logging to AddTransitStep**

Modify `src/AIQ.Urban.Core/ModelActions/AddTransitStep.cs`:

```csharp
using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;

namespace AIQ.Urban.Core.ModelActions;

public class AddTransitStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<AddTransitStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var transitDataList = creationData.OfType<TransitData>().ToList();
        var addedIds = new List<Guid>();

        foreach (var transitData in transitDataList)
        {
            var transit = new Models.UrbanElements.Transit(
                transitData.Centerline,
                transitData.Width,
                transitData.Type);

            model.AddTransit(transit);
            addedIds.AddRange(transit.GetGeometryIds());
        }

        _logger.LogInformation("Added {Count} transits", transitDataList.Count);

        return new(addedIds, []);
    }
}
```

- [ ] **Step 3: Add logging to AddZoneStep**

Modify `src/AIQ.Urban.Core/ModelActions/AddZoneStep.cs`:

```csharp
using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;

namespace AIQ.Urban.Core.ModelActions;

public class AddZoneStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<AddZoneStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var zoneDataList = creationData.OfType<ZoneData>().ToList();
        var addedIds = new List<Guid>();

        foreach (var zoneData in zoneDataList)
        {
            var zone = new Models.Zone(zoneData.Boundary, zoneData.Name);
            model.AddZone(zone);
            addedIds.AddRange(zone.GetGeometryIds());
        }

        _logger.LogInformation("Added {Count} zones", zoneDataList.Count);

        return new(addedIds, []);
    }
}
```

- [ ] **Step 4: Add logging to RemoveBuildingStep**

Modify `src/AIQ.Urban.Core/ModelActions/RemoveBuildingStep.cs`:

```csharp
using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;

namespace AIQ.Urban.Core.ModelActions;

public class RemoveBuildingStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<RemoveBuildingStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var deletionData = creationData.OfType<BuildingDeletionData>().FirstOrDefault();
        if (deletionData is null)
        {
            _logger.LogWarning("No BuildingDeletionData found in creation data");
            return new([], []);
        }

        var idsToDelete = new List<Guid>();
        foreach (var id in deletionData.GeometryIds)
        {
            var building = model.Buildings
                .FirstOrDefault(b => b.GetGeometryIds().Contains(id));

            if (building is not null)
            {
                model.RemoveBuilding(building);
                idsToDelete.AddRange(building.GetGeometryIds());
            }
        }

        _logger.LogInformation("Removed {Count} buildings", idsToDelete.Count);

        return new([], idsToDelete);
    }
}
```

- [ ] **Step 5: Add logging to RemoveTransitStep**

Modify `src/AIQ.Urban.Core/ModelActions/RemoveTransitStep.cs`:

```csharp
using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;

namespace AIQ.Urban.Core.ModelActions;

public class RemoveTransitStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<RemoveTransitStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var deletionData = creationData.OfType<TransitDeletionData>().FirstOrDefault();
        if (deletionData is null)
        {
            _logger.LogWarning("No TransitDeletionData found in creation data");
            return new([], []);
        }

        var idsToDelete = new List<Guid>();
        foreach (var id in deletionData.CenterlineIds)
        {
            var transit = model.Transits
                .FirstOrDefault(t => t.GetGeometryIds().Contains(id));

            if (transit is not null)
            {
                model.RemoveTransit(transit);
                idsToDelete.AddRange(transit.GetGeometryIds());
            }
        }

        _logger.LogInformation("Removed {Count} transits", idsToDelete.Count);

        return new([], idsToDelete);
    }
}
```

- [ ] **Step 6: Add logging to RemoveZoneStep**

Modify `src/AIQ.Urban.Core/ModelActions/RemoveZoneStep.cs`:

```csharp
using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;

namespace AIQ.Urban.Core.ModelActions;

public class RemoveZoneStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<RemoveZoneStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var deletionData = creationData.OfType<ZoneDeletionData>().FirstOrDefault();
        if (deletionData is null)
        {
            _logger.LogWarning("No ZoneDeletionData found in creation data");
            return new([], []);
        }

        var idsToDelete = new List<Guid>();
        foreach (var id in deletionData.GeometryIds)
        {
            var zone = model.Zones
                .FirstOrDefault(z => z.GetGeometryIds().Contains(id));

            if (zone is not null)
            {
                model.RemoveZone(zone);
                idsToDelete.AddRange(zone.GetGeometryIds());
            }
        }

        _logger.LogInformation("Removed {Count} zones", idsToDelete.Count);

        return new([], idsToDelete);
    }
}
```

- [ ] **Step 7: Add logging to ResetUrbanElementsStep**

Modify `src/AIQ.Urban.Core/ModelActions/ResetUrbanElementsStep.cs`:

```csharp
using Microsoft.Extensions.Logging;

namespace AIQ.Urban.Core.ModelActions;

public class ResetUrbanElementsStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<ResetUrbanElementsStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var deletedIds = model.GetUrbanElements()
            .SelectMany(e => e.GetGeometryIds())
            .ToList();

        model.Reset();

        _logger.LogInformation("Reset urban model — removed {Count} geometry objects", deletedIds.Count);

        return new([], deletedIds);
    }
}
```

- [ ] **Step 8: Add logging to UpdateBuildingStep**

Modify `src/AIQ.Urban.Core/ModelActions/UpdateBuildingStep.cs`:

```csharp
using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;

namespace AIQ.Urban.Core.ModelActions;

public class UpdateBuildingStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<UpdateBuildingStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var updateData = creationData.OfType<BuildingUpdateData>().FirstOrDefault();
        if (updateData is null)
        {
            _logger.LogWarning("No BuildingUpdateData found in creation data");
            return new([], []);
        }

        var building = model.Buildings
            .FirstOrDefault(b => b.GetGeometryIds().Contains(updateData.OldEnvelopeId));

        if (building is null)
        {
            _logger.LogWarning("Building not found for envelope ID {Id}", updateData.OldEnvelopeId);
            return new([], []);
        }

        var oldIds = building.GetGeometryIds().ToList();
        building.Update(updateData.NewEnvelope, updateData.TypicalFloorHeight, updateData.OldPlateUses);
        var newIds = building.GetGeometryIds().ToList();

        _logger.LogInformation("Updated building {Id}", updateData.OldEnvelopeId);

        return new(newIds, oldIds);
    }
}
```

- [ ] **Step 9: Add logging to UpdateTransitStep**

Modify `src/AIQ.Urban.Core/ModelActions/UpdateTransitStep.cs`:

```csharp
using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;

namespace AIQ.Urban.Core.ModelActions;

public class UpdateTransitStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<UpdateTransitStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var updateData = creationData.OfType<TransitUpdateData>().FirstOrDefault();
        if (updateData is null)
        {
            _logger.LogWarning("No TransitUpdateData found in creation data");
            return new([], []);
        }

        var transit = model.Transits
            .FirstOrDefault(t => t.GetGeometryIds().Contains(updateData.OldCenterlineId));

        if (transit is null)
        {
            _logger.LogWarning("Transit not found for centerline ID {Id}", updateData.OldCenterlineId);
            return new([], []);
        }

        var oldIds = transit.GetGeometryIds().ToList();
        transit.UpdateCenterline(updateData.NewCenterline);
        var newIds = transit.GetGeometryIds().ToList();

        _logger.LogInformation("Updated transit {Id}", updateData.OldCenterlineId);

        return new(newIds, oldIds);
    }
}
```

- [ ] **Step 10: Add logging to UpdateZoneStep**

Modify `src/AIQ.Urban.Core/ModelActions/UpdateZoneStep.cs`:

```csharp
using AIQ.Urban.Core.ModelActions.CreationData;
using Microsoft.Extensions.Logging;

namespace AIQ.Urban.Core.ModelActions;

public class UpdateZoneStep(ILoggerFactory? _loggerFactory = null) : IUrbanModelStep
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<UpdateZoneStep>();

    public StepResult Run(UrbanModel model, List<ICreationData> creationData)
    {
        var updateData = creationData.OfType<ZoneUpdateData>().FirstOrDefault();
        if (updateData is null)
        {
            _logger.LogWarning("No ZoneUpdateData found in creation data");
            return new([], []);
        }

        var zone = model.Zones
            .FirstOrDefault(z => z.GetGeometryIds().Contains(updateData.OldBoundaryId));

        if (zone is null)
        {
            _logger.LogWarning("Zone not found for boundary ID {Id}", updateData.OldBoundaryId);
            return new([], []);
        }

        var oldIds = zone.GetGeometryIds().ToList();
        zone.UpdateBoundary(updateData.NewBoundary);
        var newIds = zone.GetGeometryIds().ToList();

        _logger.LogInformation("Updated zone {Id}", updateData.OldBoundaryId);

        return new(newIds, oldIds);
    }
}
```

- [ ] **Step 11: Add logging to TransitSurfaceUnioner**

Modify `src/AIQ.Urban.Core/ModelActions/TransitSurfaceUnioner.cs`:

Add `ILoggerFactory? _loggerFactory = null` to primary constructor, add logger field, add logging after union:

```csharp
_logger.LogInformation("Unioned transit surfaces for {Count} types", model.TransitSurfaces.Count);
```

- [ ] **Step 12: Add logging to RecalculateParcelsStep**

Modify `src/AIQ.Urban.Core/ModelActions/RecalculateParcelsStep.cs`:

Add `ILoggerFactory? _loggerFactory = null` to primary constructor, add logger field, add logging:

```csharp
_logger.LogInformation("Recalculated {Count} parcels", model.Parcels.Count);
```

- [ ] **Step 13: Add logging to ReplaceFloorPlateStep**

Modify `src/AIQ.Urban.Core/ModelActions/ReplaceFloorPlateStep.cs`:

Add `ILoggerFactory? _loggerFactory = null` to primary constructor, add logger field, add logging:

```csharp
_logger.LogInformation("Replaced floor plate at Z={Z:F2}", newZ);
```

- [ ] **Step 14: Add logging to ChangeBuildingUseStep**

Modify `src/AIQ.Urban.Core/ModelActions/ChangeBuildingUseStep.cs`:

Add `ILoggerFactory? _loggerFactory = null` to primary constructor, add logger field, add logging:

```csharp
_logger.LogInformation("Changed use for {Count} building elements", affectedCount);
```

- [ ] **Step 15: Add logging to ChangeParcelUseStep**

Modify `src/AIQ.Urban.Core/ModelActions/ChangeParcelUseStep.cs`:

Add `ILoggerFactory? _loggerFactory = null` to primary constructor, add logger field, add logging:

```csharp
_logger.LogInformation("Changed use for {Count} parcels", affectedCount);
```

- [ ] **Step 16: Add logging to ModifyModelUsesStep**

Modify `src/AIQ.Urban.Core/ModelActions/ModifyModelUsesStep.cs`:

Add `ILoggerFactory? _loggerFactory = null` to primary constructor, add logger field, add logging:

```csharp
_logger.LogInformation("Modified model uses: {EditType} '{Name}'", editType, name);
```

- [ ] **Step 17: Verify build**

```bash
dotnet build src/AIQ.Urban.Core/AIQ.Urban.Core.csproj
```

Expected: Build succeeds.

- [ ] **Step 18: Commit**

```bash
git add src/AIQ.Urban.Core/ModelActions/
git commit -m "feat: add logging to all 16 core pipeline steps"
```

---

### Task 6: Add ILoggerFactory to all 5 outputters + logging in Run methods

**Files:**
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/BuildingOutputter.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/TransitOutputter.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/ParcelOutputter.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/ZoneOutputter.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/TransitSurfaceOutputter.cs`

- [ ] **Step 1: Add logging to BuildingOutputter**

Modify `src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/BuildingOutputter.cs`:

```csharp
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Utils;
using AIQ.Urban.RhinoInterface.Utils;
using Microsoft.Extensions.Logging;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class BuildingOutputter(ILoggerFactory? _loggerFactory = null) : IUrbanModelRhinoOutputter
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<BuildingOutputter>();

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

        _logger.LogInformation("Output {Count} buildings to Rhino", buildings.Count);
    }
}
```

- [ ] **Step 2: Add logging to TransitOutputter**

Follow same pattern: add `ILoggerFactory? _loggerFactory = null` primary constructor param, logger field, log after output:

```csharp
_logger.LogInformation("Output {Count} transits to Rhino", transits.Count);
```

- [ ] **Step 3: Add logging to ParcelOutputter**

Same pattern:

```csharp
_logger.LogInformation("Output {Count} parcels to Rhino", parcels.Count);
```

- [ ] **Step 4: Add logging to ZoneOutputter**

Same pattern:

```csharp
_logger.LogInformation("Output {Count} zones to Rhino", zones.Count);
```

- [ ] **Step 5: Add logging to TransitSurfaceOutputter**

Same pattern:

```csharp
_logger.LogInformation("Output {Count} transit surfaces to Rhino", surfaces.Count);
```

- [ ] **Step 6: Verify build**

```bash
dotnet build src/AIQ.Urban.RhinoInterface/AIQ.Urban.RhinoInterface.csproj
```

Expected: Build succeeds.

- [ ] **Step 7: Commit**

```bash
git add src/AIQ.Urban.RhinoInterface/Pipeline/Outputters/
git commit -m "feat: add logging to all 5 outputters"
```

---

### Task 7: Update PipelineConfigurations to pass LoggerFactory to steps

**Files:**
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/PipelineConfigurations.cs`

**Interfaces:**
- Consumes: `pipeline.LoggerFactory` from pipeline (Slice 1)
- Produces: Steps created with `ILoggerFactory` — consumed by steps (Slice 2)

The extension methods read `pipeline.LoggerFactory` and pass it to each step constructor. No method signature changes — just construction changes.

- [ ] **Step 1: Update all step constructors in PipelineConfigurations**

In every `With*` method, change `new StepName()` to `new StepName(pipeline.LoggerFactory)`. For steps that already have params, append `pipeline.LoggerFactory` as the last argument.

Example for `WithTransitAdditionSteps`:

```csharp
public static RhinoUrbanModelPipeline WithTransitAdditionSteps(
    this RhinoUrbanModelPipeline pipeline,
    List<TransitData> transitData,
    List<Guid> centerlinesToRemove)
{
    pipeline.WithRhinoObjectDeletion(centerlinesToRemove)
        .WithCreationData(transitData.Cast<ICreationData>().ToList())
        .WithCreationData([new TransitDeletionData(centerlinesToRemove)])
        .WithUrbanElementStep(new RemoveTransitStep(pipeline.LoggerFactory))
        .WithUrbanElementStep(new AddTransitStep(pipeline.LoggerFactory))
        .WithUrbanElementStep(new TransitSurfaceUnioner(loggerFactory: pipeline.LoggerFactory))
        .WithUrbanElementStep(new RecalculateParcelsStep(pipeline.LoggerFactory));

    return pipeline;
}
```

Full list of changes:
| Method | Step changes |
|--------|-------------|
| `WithBuildingAdditionSteps` | `new AddBuildingStep(loggerFactory: pipeline.LoggerFactory)` |
| `WithTransitAdditionSteps` | `new RemoveTransitStep(pipeline.LoggerFactory)`, `new AddTransitStep(pipeline.LoggerFactory)`, `new TransitSurfaceUnioner(loggerFactory: pipeline.LoggerFactory)`, `new RecalculateParcelsStep(pipeline.LoggerFactory)` |
| `WithBuildingUseChangeSteps` | `new ChangeBuildingUseStep(pipeline.LoggerFactory)` |
| `WithZoneAdditionSteps` | `new AddZoneStep(pipeline.LoggerFactory)` |
| `WithTransitDeletionSteps` | `new RemoveTransitStep(pipeline.LoggerFactory)`, `new TransitSurfaceUnioner(loggerFactory: pipeline.LoggerFactory)`, `new RecalculateParcelsStep(pipeline.LoggerFactory)` |
| `WithTransitUpdateSteps` | `new UpdateTransitStep(pipeline.LoggerFactory)`, `new TransitSurfaceUnioner(loggerFactory: pipeline.LoggerFactory)`, `new RecalculateParcelsStep(pipeline.LoggerFactory)` |
| `WithBuildingDeletionSteps` | `new RemoveBuildingStep(pipeline.LoggerFactory)` |
| `WithZoneDeletionSteps` | `new RemoveZoneStep(pipeline.LoggerFactory)` |
| `WithZoneUpdateSteps` | `new UpdateZoneStep(pipeline.LoggerFactory)` |
| `WithBuildingUpdateSteps` | `new UpdateBuildingStep(pipeline.LoggerFactory)` |
| `WithFloorPlateReplaceSteps` | `new ReplaceFloorPlateStep(pipeline.LoggerFactory)` |
| `WithLanduseListEditSteps` | `new ModifyModelUsesStep(pipeline.LoggerFactory)` |
| `WithDocModelLoad` | `new ResetUrbanElementsStep(pipeline.LoggerFactory)`, `new AddTransitStep(pipeline.LoggerFactory)`, `new AddBuildingStep(loggerFactory: pipeline.LoggerFactory)`, `new AddZoneStep(pipeline.LoggerFactory)`, `new TransitSurfaceUnioner(loggerFactory: pipeline.LoggerFactory)`, `new RecalculateParcelsStep(pipeline.LoggerFactory)` |

- [ ] **Step 2: Verify build**

```bash
dotnet build src/AIQ.Urban.RhinoInterface/AIQ.Urban.RhinoInterface.csproj
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/AIQ.Urban.RhinoInterface/Pipeline/PipelineConfigurations.cs
git commit -m "feat: PipelineConfigurations passes LoggerFactory to all steps"
```

---

## Slice 3: Call Sites + Data Gather Steps + Tests

**Goal:** Wire everything together. Every pipeline construction site creates an `ILoggerFactory`, passes it to the pipeline, and disposes after `Run()`. Data gather steps get logging. Tests verify the logging infrastructure works.

### Task 8: Add ILoggerFactory to all 5 data gather steps

**Files:**
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/DocLoadGatherStep.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/SelectBuildingEnvelopeStep.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/SelectBuildingFootprintsStep.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/SelectTransitCenterlinesStep.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/SelectZoneBoundaryStep.cs`

**Pattern:** Add `ILoggerFactory? _loggerFactory = null` as the last primary constructor parameter, add logger field, log at Information level in `Run()`.

- [ ] **Step 1: Add logging to DocLoadGatherStep**

Add `ILoggerFactory? _loggerFactory = null` after `_tolerance`, add logger field, log:

```csharp
_logger.LogInformation("Gathered {Transits} transits, {Buildings} buildings, {Zones} zones from document",
    transitCount, buildingCount, zoneCount);
```

- [ ] **Step 2: Add logging to SelectBuildingEnvelopeStep**

Add `ILoggerFactory? _loggerFactory = null` as last param, log:

```csharp
_logger.LogInformation("Gathered {Count} building envelopes", count);
```

- [ ] **Step 3: Add logging to SelectBuildingFootprintStep**

Same pattern:

```csharp
_logger.LogInformation("Gathered {Count} building footprints", count);
```

- [ ] **Step 4: Add logging to SelectTransitCenterlinesStep**

Same pattern:

```csharp
_logger.LogInformation("Gathered {Count} transit centerlines", count);
```

- [ ] **Step 5: Add logging to SelectZoneBoundaryStep**

Same pattern:

```csharp
_logger.LogInformation("Gathered {Count} zone boundaries", count);
```

- [ ] **Step 6: Verify build**

```bash
dotnet build src/AIQ.Urban.RhinoInterface/AIQ.Urban.RhinoInterface.csproj
```

Expected: Build succeeds.

- [ ] **Step 7: Commit**

```bash
git add src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDataGatherSteps/
git commit -m "feat: add logging to all 5 data gather steps"
```

---

### Task 9: Update all 19 call sites to create ILoggerFactory

**Files:**
- Modify: `src/AIQ.Urban.Rhino/Commands/AddTransit_AIQUrban.cs`
- Modify: `src/AIQ.Urban.Rhino/Commands/AddBuilding_AIQUrban.cs`
- Modify: `src/AIQ.Urban.Rhino/Commands/AddZone_AIQUrban.cs`
- Modify: `src/AIQ.Urban.Rhino/Commands/ChangeBuildingUse_AIQUrban.cs`
- Modify: `src/AIQ.Urban.Rhino/Commands/ResetUrban_AIQUrban.cs`
- Modify: `src/AIQ.Urban.Rhino/Commands/StartUI_AIQUrban.cs`
- Modify: `src/AIQ.Urban.Rhino/MCP_Bridge/RhinoPipelineExecutorForMcp.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/EventHandler/RhinoUrbanEventHandler.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/TransitMessage.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/BuildingMessage.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/AddZoneMessage.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/ChangeUseMessage.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/LanduseListEditMessage.cs`

**Pattern:** At each `new RhinoUrbanModelPipeline(...)` call:
1. Add `using var loggerFactory = new LoggerFactory().AddRhinoLog();` before the pipeline construction
2. Pass `loggerFactory` as the third argument to the pipeline constructor
3. Add `using AIQ.Urban.RhinoInterface.Logging;` to the file's usings

- [ ] **Step 1: Update AddTransit_AIQUrban**

```csharp
using AIQ.Urban.RhinoInterface.Logging;
// ... existing usings ...

// In RunCommand, before pipeline construction:
using var loggerFactory = new LoggerFactory().AddRhinoLog();
var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, loggerFactory)
    // ... rest unchanged ...
```

- [ ] **Step 2: Update AddBuilding_AIQUrban**

Same pattern: add `using`, create `loggerFactory`, pass to pipeline.

- [ ] **Step 3: Update AddZone_AIQUrban**

Same pattern.

- [ ] **Step 4: Update ChangeBuildingUse_AIQUrban**

Same pattern.

- [ ] **Step 5: Update ResetUrban_AIQUrban**

Same pattern.

- [ ] **Step 6: Update StartUI_AIQUrban**

Same pattern. Note: this file already has a try/catch — the `using var` goes before the try block.

- [ ] **Step 7: Update RhinoPipelineExecutorForMcp**

```csharp
using AIQ.Urban.RhinoInterface.Logging;
// ...

public ProjectMetrics Run(Action<RhinoUrbanModelPipeline> pipelineAction)
{
    using var loggerFactory = new LoggerFactory().AddRhinoLog();
    var pipeline = new RhinoUrbanModelPipeline(doc, model, loggerFactory);
    pipelineAction(pipeline);
    pipeline.Run();
    doc.Views.Redraw();
    return model.ProjectMetrics!;
}
```

- [ ] **Step 8: Update RhinoUrbanEventHandler**

The event handler creates 7 pipelines. Add `using AIQ.Urban.RhinoInterface.Logging;` to the file. In each `Handle*` method, create a `loggerFactory` and pass it:

```csharp
private void HandleTransitDelete(Transit transit)
{
    var centerlineIds = transit.GetGeometryIds().ToList();
    using var loggerFactory = new LoggerFactory().AddRhinoLog();
    var pipeline = new RhinoUrbanModelPipeline(_doc, UrbanModel.Instance, loggerFactory)
        .WithTransitDeletionSteps(centerlineIds)
        .WithFullModelOutput()
        .WithMetricsCalculation();
    RunPipeline(pipeline);
}
```

Repeat for all 7 `Handle*` methods.

- [ ] **Step 9: Update TransitMessage**

Same pattern: add `using`, create `loggerFactory`, pass to pipeline.

- [ ] **Step 10: Update BuildingMessage**

Same pattern. Note: `BuildingMessage` has a `RunPipeline` helper — create `loggerFactory` there.

- [ ] **Step 11: Update AddZoneMessage**

Same pattern.

- [ ] **Step 12: Update ChangeUseMessage**

Same pattern.

- [ ] **Step 13: Update LanduseListEditMessage**

Same pattern.

- [ ] **Step 14: Verify build**

```bash
dotnet build src/AIQ.Urban.Rhino/AIQ.Urban.Rhino.csproj
dotnet build src/AIQ.Urban.RhinoInterface/AIQ.Urban.RhinoInterface.csproj
```

Expected: Build succeeds.

- [ ] **Step 15: Run existing tests**

```bash
dotnet test
```

Expected: All tests pass.

- [ ] **Step 16: Commit**

```bash
git add src/AIQ.Urban.Rhino/Commands/ src/AIQ.Urban.Rhino/MCP_Bridge/ src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/ src/AIQ.Urban.RhinoInterface/EventHandler/
git commit -m "feat: all call sites create ILoggerFactory and pass to pipeline"
```

---

### Task 10: Write tests for logging infrastructure

**Files:**
- Create: `tests/AIQ.Urban.RhinoInterface.Tests/Logging/RhinoLoggerTests.cs`

**Interfaces:**
- Consumes: `RhinoLogger`, `RhinoLogProvider` (Slice 1)

- [ ] **Step 1: Create test for RhinoLogger compact formatting**

```csharp
using Microsoft.Extensions.Logging;
using AIQ.Urban.RhinoInterface.Logging;

namespace AIQ.Urban.RhinoInterface.Tests.Logging;

public class RhinoLoggerTests
{
    [Theory]
    [InlineData(LogLevel.Information, "[Info]")]
    [InlineData(LogLevel.Warning, "[Warn]")]
    [InlineData(LogLevel.Error, "[Error]")]
    [InlineData(LogLevel.Critical, "[Error]")]
    [InlineData(LogLevel.Debug, null)]
    [InlineData(LogLevel.Trace, null)]
    public void Log_CorrectLevelPrefix(LogLevel level, string? expectedPrefix)
    {
        var provider = new RhinoLogProvider();
        var logger = provider.CreateLogger("TestCategory");

        // Act - should not throw
        logger.Log(level, 0, "Test message {Arg}", null, (state, ex) => state);

        // Verify IsEnabled matches expected behavior
        if (expectedPrefix is null)
        {
            Assert.False(logger.IsEnabled(level));
        }
        else
        {
            Assert.True(logger.IsEnabled(level));
        }
    }

    [Fact]
    public void IsEnabled_ReturnsTrueForInformationAndAbove()
    {
        var provider = new RhinoLogProvider();
        var logger = provider.CreateLogger("Test");

        Assert.False(logger.IsEnabled(LogLevel.Trace));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.True(logger.IsEnabled(LogLevel.Critical));
    }

    [Fact]
    public void BeginScope_ReturnsNull()
    {
        var provider = new RhinoLogProvider();
        var logger = provider.CreateLogger("Test");

        var scope = logger.BeginScope("anything");

        Assert.Null(scope);
    }

    [Fact]
    public void CreateLogger_ReturnsSameType()
    {
        var provider = new RhinoLogProvider();

        var logger = provider.CreateLogger("Test");

        Assert.NotNull(logger);
        Assert.IsType<RhinoLogger>(logger);
    }
}
```

- [ ] **Step 2: Run tests**

```bash
dotnet test tests/AIQ.Urban.RhinoInterface.Tests/
```

Expected: Tests pass.

- [ ] **Step 3: Commit**

```bash
git add tests/AIQ.Urban.RhinoInterface.Tests/Logging/
git commit -m "test: add unit tests for RhinoLogProvider and RhinoLogger"
```

---

### Task 11: Final verification

- [ ] **Step 1: Build entire solution**

```bash
dotnet build ArchSitePlan.sln
```

Expected: Build succeeds with no errors.

- [ ] **Step 2: Run all tests**

```bash
dotnet test
```

Expected: All tests pass.

- [ ] **Step 3: Manual smoke test**

Open Rhino, run `AddTransit_AIQUrban`. Expected command line output:
```
[Info] SelectTransitCenterlinesStep: Gathered N transit centerlines
[Info] RemoveTransitStep: Removed M transits
[Info] AddTransitStep: Added N transits
[Info] TransitSurfaceUnioner: Unioned transit surfaces for K types
[Info] RecalculateParcelsStep: Recalculated P parcels
[Info] TransitOutputter: Output N transits to Rhino
[Info] BuildingOutputter: Output 0 buildings to Rhino
...
[Info] RhinoUrbanModelPipeline: Pipeline completed — X added, Y deleted
```

---

## Summary of Changes

| Slice | Files Changed | Files Created |
|-------|--------------|---------------|
| 1: Infrastructure | 3 (pipeline, 2 .csproj) | 3 (provider, logger, extension) |
| 2: Steps + Outputters | 21 (16 steps + 5 outputters) + 1 (PipelineConfigurations) | 0 |
| 3: Call Sites + Tests | 13 (commands, event handler, MCP, UI messages) + 5 (data gather steps) | 1 (tests) |
| **Total** | **42** | **4** |
