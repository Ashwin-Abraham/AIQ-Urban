# Migrate Urban Element Creation Data to `ICreationData` + `IRhinoDataGatherStep`

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `BuildingData`, `TransitData`, and `ZoneData` implement `ICreationData`, move Rhino selection/value gathering into `IRhinoDataGatherStep`, feed creation data through `IAddUrbanElementsStep.Run`, and restore a successful solution build.

**Architecture:** A generic `CreationDataCollection<T>` carries lists of creation records. The pipeline resolves `ICreationData` either from a configured `IRhinoDataGatherStep` or from directly provided data. `IUrbanModelStep.Run` now accepts the nullable data so delete-only pipelines can still run. Existing `PipelineConfigurations` signatures stay backward-compatible for tests and MCP callers.

**Tech Stack:** .NET 8, C# 12, RhinoCommon, NUnit/Rhino.Testing.

## Global Constraints

- All projects use nullable reference types (`<Nullable>enable</Nullable>`).
- `AIQ.Urban.Core` must not depend on other solution projects.
- `AIQ.Urban.RhinoInterface` references `AIQ.Urban.Core` and RhinoCommon.
- `AIQ.Urban.RhinoPlugin` references `AIQ.Urban.RhinoInterface` and owns Rhino commands/UI messages.
- File-scoped namespaces, one class per file.
- The solution must build at the end of the plan; tests do not need to be run.

---

## File Structure

| File | Responsibility |
|------|----------------|
| `src/AIQ.Urban.Core/ModelActions/CreationData/TransitData.cs` | Creation record for transit, implements `ICreationData` |
| `src/AIQ.Urban.Core/ModelActions/CreationData/ZoneData.cs` | Creation record for zone, implements `ICreationData` |
| `src/AIQ.Urban.Core/ModelActions/CreationData/CreationDataCollection.cs` | Generic list wrapper implementing `ICreationData` |
| `src/AIQ.Urban.Core/ModelActions/IUrbanModelStep.cs` | Base step interface with `Run(UrbanModel, ICreationData?)` |
| `src/AIQ.Urban.Core/ModelActions/AddBuildingStep.cs` | Receives `ICreationData`, casts to collection, adds buildings |
| `src/AIQ.Urban.Core/ModelActions/AddTransitStep.cs` | Receives `ICreationData`, casts to collection, adds transits |
| `src/AIQ.Urban.Core/ModelActions/AddZoneStep.cs` | Receives `ICreationData`, casts to collection, adds zones |
| `src/AIQ.Urban.Core/ModelActions/Remove*Step.cs` | Accepts `ICreationData?` but ignores it |
| `src/AIQ.Urban.Core/ModelActions/ResetUrbanElementsStep.cs` | Accepts `ICreationData?` but ignores it |
| `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoUrbanModelPipeline.cs` | Resolves and passes creation data to steps |
| `src/AIQ.Urban.RhinoInterface/Pipeline/PipelineConfigurations.cs` | Wraps provided data in `CreationDataCollection<T>` |
| `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDocActions/SelectTransitCenterlinesStep.cs` | Gathers curves and builds `CreationDataCollection<TransitData>` |
| `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDocActions/SelectBuildingFootprintsStep.cs` | Gathers curves/Breps and builds `CreationDataCollection<BuildingData>` |
| `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDocActions/SelectZoneBoundaryStep.cs` | Gathers curve and builds `CreationDataCollection<ZoneData>` |
| `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDocActions/SelectCurveStep.cs` | **Delete** — replaced by typed gather steps |
| `src/AIQ.Urban.Rhino/Commands/Add*_*_AIQUrban.cs` | Use gather steps and pass extracted IDs to configurations |
| `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/*Message.cs` | Use gather steps and pass extracted IDs to configurations |
| `tests/AIQ.Urban.Core.Tests/ModelActions/Add*StepTests.cs` | Update `Run` calls to pass `CreationDataCollection<T>` |
| `tests/AIQ.Urban.RhinoPlugin.Tests/ResetUrban_AIQUrbanTests.cs` | Update `Run` calls where steps are used directly |

---

## Task 1: Move Creation Data Records and Add Collection Wrapper

**Files:**
- Create: `src/AIQ.Urban.Core/ModelActions/CreationData/TransitData.cs`
- Create: `src/AIQ.Urban.Core/ModelActions/CreationData/ZoneData.cs`
- Create: `src/AIQ.Urban.Core/ModelActions/CreationData/CreationDataCollection.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/AddTransitStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/AddZoneStep.cs`

**Interfaces:**
- Consumes: `ICreationData` marker interface
- Produces: `TransitData`, `ZoneData`, `CreationDataCollection<T>` all implementing `ICreationData`

- [ ] **Step 1: Create `TransitData.cs`**

```csharp
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record TransitData(
     Curve2D Centerline,
     double Width,
     TransitType TransitType) : ICreationData
{
    public Transit ToTransit()
    {
        return new Transit(Centerline, Width, TransitType);
    }
}
```

- [ ] **Step 2: Create `ZoneData.cs`**

```csharp
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record ZoneData(Polygon2D Boundary, string Name) : ICreationData
{
    public Zone ToZone()
    {
        return new Zone(Boundary, Name);
    }
}
```

- [ ] **Step 3: Create `CreationDataCollection.cs`**

```csharp
namespace AIQ.Urban.Core.ModelActions.CreationData;

public record CreationDataCollection<T>(List<T> Items) : ICreationData
    where T : ICreationData;
```

- [ ] **Step 4: Remove `TransitData` record from `AddTransitStep.cs`**

Replace the entire file with:

```csharp
using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class AddTransitStep : IAddUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model, ICreationData? creationData)
    {
        var collection = creationData as CreationDataCollection<TransitData>
            ?? throw new InvalidOperationException("Expected CreationDataCollection<TransitData>.");

        var transits = collection.Items.Select(x => x.ToTransit()).ToList();
        model.AddTransit(transits);

        return transits.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}
```

- [ ] **Step 5: Remove `ZoneData` record from `AddZoneStep.cs`**

Replace the entire file with:

```csharp
using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class AddZoneStep : IAddUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model, ICreationData? creationData)
    {
        var collection = creationData as CreationDataCollection<ZoneData>
            ?? throw new InvalidOperationException("Expected CreationDataCollection<ZoneData>.");

        var zones = collection.Items.Select(x => x.ToZone()).ToList();
        foreach (var zone in zones)
            model.AddZone(zone);

        return zones.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add src/AIQ.Urban.Core/ModelActions/CreationData/TransitData.cs

git add src/AIQ.Urban.Core/ModelActions/CreationData/ZoneData.cs

git add src/AIQ.Urban.Core/ModelActions/CreationData/CreationDataCollection.cs

git add src/AIQ.Urban.Core/ModelActions/AddTransitStep.cs

git add src/AIQ.Urban.Core/ModelActions/AddZoneStep.cs

git commit -m "refactor: move TransitData and ZoneData to CreationData and add collection wrapper"
```

---

## Task 2: Update Base Step Interface and All Implementations

**Files:**
- Modify: `src/AIQ.Urban.Core/ModelActions/IUrbanModelStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/AddBuildingStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/RemoveBuildingStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/RemoveTransitStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/RemoveZoneStep.cs`
- Modify: `src/AIQ.Urban.Core/ModelActions/ResetUrbanElementsStep.cs`

**Interfaces:**
- Consumes: `ICreationData`, `CreationDataCollection<T>`
- Produces: `IUrbanModelStep.Run(UrbanModel, ICreationData?)` implementation for all add/delete steps

- [ ] **Step 1: Update `IUrbanModelStep.cs`**

```csharp
using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public interface IUrbanModelStep
{
    public List<Guid> Run(UrbanModel model, ICreationData? creationData);
}

public interface IAddUrbanElementsStep : IUrbanModelStep;
public interface IDeleteUrbanElementsStep : IUrbanModelStep;

public record EditStepResult(List<Guid> AddedIds, List<Guid> DeletedIds);

public interface IEditUrbanElementsStep
{
    public EditStepResult Run(UrbanModel model);
}
```

- [ ] **Step 2: Update `AddBuildingStep.cs`**

```csharp
using AIQ.Urban.Core.ModelActions.CreationData;

namespace AIQ.Urban.Core.ModelActions;

public class AddBuildingStep : IAddUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model, ICreationData? creationData)
    {
        var collection = creationData as CreationDataCollection<BuildingData>
            ?? throw new InvalidOperationException("Expected CreationDataCollection<BuildingData>.");

        var buildings = collection.Items.Select(x => x.ToBuilding()).ToList();
        foreach (var building in buildings)
            model.AddBuilding(building);

        return buildings.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}
```

- [ ] **Step 3: Update `RemoveBuildingStep.cs`**

```csharp
namespace AIQ.Urban.Core.ModelActions;

public class RemoveBuildingStep(List<Guid> _geometryIds) : IDeleteUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model, ICreationData? creationData)
    {
        var buildingsToRemove = model.Buildings
            .Where(x => x.GetGeometryIds()
                .Any(id => _geometryIds.Contains(id)))
            .ToList();

        model.Buildings.RemoveAll(x => buildingsToRemove.Contains(x));

        return buildingsToRemove.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}
```

- [ ] **Step 4: Update `RemoveTransitStep.cs`**

```csharp
namespace AIQ.Urban.Core.ModelActions;

public class RemoveTransitStep(List<Guid> _centerlineIds) : IDeleteUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model, ICreationData? creationData)
    {
        var transitsToRemove = model.Transits
            .Where(x => x.GetGeometryIds()
                .Any(id => _centerlineIds.Contains(id)))
            .ToList();

        model.Transits.RemoveAll(x => transitsToRemove.Contains(x));

        return transitsToRemove.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}
```

- [ ] **Step 5: Update `RemoveZoneStep.cs`**

```csharp
namespace AIQ.Urban.Core.ModelActions;

public class RemoveZoneStep(List<Guid> _geometryIds) : IDeleteUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model, ICreationData? creationData)
    {
        var zonesToRemove = model.Zones
            .Where(x => x.GetGeometryIds()
                .Any(id => _geometryIds.Contains(id)))
            .ToList();

        model.Zones.RemoveAll(x => zonesToRemove.Contains(x));

        return zonesToRemove.SelectMany(x => x.GetGeometryIds()).ToList();
    }
}
```

- [ ] **Step 6: Update `ResetUrbanElementsStep.cs`**

```csharp
namespace AIQ.Urban.Core.ModelActions;

public class ResetUrbanElementsStep : IDeleteUrbanElementsStep
{
    List<Guid> IUrbanModelStep.Run(UrbanModel model, ICreationData? creationData)
    {
        var ids = model.GetUrbanElements()
            .SelectMany(e => e.GetGeometryIds())
            .Concat(model.TransitSurfaces.Select(ts => ts.Profile.Id))
            .ToList();

        model.Reset();

        return ids;
    }
}
```

- [ ] **Step 7: Commit**

```bash
git add src/AIQ.Urban.Core/ModelActions/IUrbanModelStep.cs

git add src/AIQ.Urban.Core/ModelActions/AddBuildingStep.cs

git add src/AIQ.Urban.Core/ModelActions/RemoveBuildingStep.cs

git add src/AIQ.Urban.Core/ModelActions/RemoveTransitStep.cs

git add src/AIQ.Urban.Core/ModelActions/RemoveZoneStep.cs

git add src/AIQ.Urban.Core/ModelActions/ResetUrbanElementsStep.cs

git commit -m "refactor: update IUrbanModelStep and all add/delete implementations"
```

---

## Task 3: Update Rhino Pipeline to Resolve and Pass Creation Data

**Files:**
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoUrbanModelPipeline.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/Pipeline/PipelineConfigurations.cs`

**Interfaces:**
- Consumes: `IRhinoDataGatherStep`, `ICreationData`, `CreationDataCollection<T>`
- Produces: `WithDataGatherStep`, `WithCreationData`, resolved `ICreationData?` passed to steps

- [ ] **Step 1: Update `RhinoUrbanModelPipeline.cs`**

```csharp
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.EventHandler;
using AIQ.Urban.RhinoInterface.Pipeline.Outputters;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions;
using Rhino;

namespace AIQ.Urban.RhinoInterface;

public class RhinoUrbanModelPipeline(
    RhinoDoc _rhinoDoc,
    UrbanModel _urbanModel)
{
    private IRhinoDataGatherStep? _objectSelectionStep;
    private ICreationData? _creationData;
    private readonly List<IUrbanModelStep> _addDeleteSteps = [];
    private readonly List<IEditUrbanElementsStep> _editSteps = [];
    private readonly List<Guid> _directDeleteIds = [];
    private readonly List<Action<UrbanModel>> _postActions = [];

    private RhinoUrbanEventHandler? _eventHandler = null;

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

    public RhinoUrbanModelPipeline WithCreationData(ICreationData data)
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

        if (creationData is null && _addDeleteSteps.OfType<IAddUrbanElementsStep>().Any())
            throw new InvalidOperationException("No creation data provided for add steps.");

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
            var result = step.Run(_urbanModel);
            addedIds.AddRange(result.AddedIds);
            deletedIds.AddRange(result.DeletedIds);
        }

        deletedIds.AddRange(_directDeleteIds);
        DeleteRhinoObjects(deletedIds);
        AddRhinoObjects(addedIds);

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

        var outputters = new List<IUrbanModelRhinoOutputter>
        {
            new TransitOutputter(elementsToAdd.OfType<Transit>().ToList()),
            new BuildingOutputter(elementsToAdd.OfType<Building>().ToList()),
            new ParcelOutputter(elementsToAdd.OfType<Parcel>().ToList()),
            new ZoneOutputter(elementsToAdd.OfType<Zone>().ToList()),
            new TransitSurfaceOutputter()
        };

        foreach (var outputter in outputters)
            outputter.Run(_urbanModel, _rhinoDoc);
    }

    private List<IUrbanElement> FilterElementsToAdd(List<Guid> addedIds)
    {
        var elementsToAdd = new List<IUrbanElement>();
        var allElements = _urbanModel.GetUrbanElements();

        foreach (var element in allElements)
        {
            var ids = element.GetGeometryIds();

            if (!ids.Any(addedIds.Contains))
                continue;

            elementsToAdd.Add(element);
        }

        return elementsToAdd;
    }

    private void DeleteRhinoObjects(List<Guid> idsToDelete)
    {
        var deletableObjects = _rhinoDoc.Objects.Where(o => idsToDelete.Contains(o.Id)).ToList();
        deletableObjects.ForEach(obj => _rhinoDoc.Objects.Delete(obj, true, true));
    }
}
```

- [ ] **Step 2: Update `PipelineConfigurations.cs`**

Add `using AIQ.Urban.Core.ModelActions.CreationData;` if not already present.

Update `WithBuildingAdditionSteps`:

```csharp
public static RhinoUrbanModelPipeline WithBuildingAdditionSteps(
    this RhinoUrbanModelPipeline pipeline,
    List<BuildingData> buildingData,
    List<Guid> curvesToDelete)
{
    var addBuildingStep = new AddBuildingStep();

    pipeline.WithRhinoObjectDeletion(curvesToDelete)
        .WithCreationData(new CreationDataCollection<BuildingData>(buildingData))
        .WithUrbanElementStep(addBuildingStep);

    return pipeline;
}
```

Update `WithTransitAdditionSteps`:

```csharp
public static RhinoUrbanModelPipeline WithTransitAdditionSteps(
    this RhinoUrbanModelPipeline pipeline,
    List<TransitData> transitData,
    List<Guid> centerlinesToRemove)
{
    var addTransitStep = new AddTransitStep();
    var removeExistingTransitStep = new RemoveTransitStep(centerlinesToRemove);
    var transitUnioner = new TransitSurfaceUnioner();
    var recalculateParcelsStep = new RecalculateParcelsStep();

    pipeline.WithRhinoObjectDeletion(centerlinesToRemove)
        .WithCreationData(new CreationDataCollection<TransitData>(transitData))
        .WithUrbanElementStep(removeExistingTransitStep)
        .WithUrbanElementStep(addTransitStep)
        .WithUrbanElementStep(transitUnioner)
        .WithUrbanElementStep(recalculateParcelsStep);

    return pipeline;
}
```

Update `WithZoneAdditionSteps`:

```csharp
public static RhinoUrbanModelPipeline WithZoneAdditionSteps(
    this RhinoUrbanModelPipeline pipeline,
    List<ZoneData> zoneData,
    List<Guid> idsToDelete)
{
    var addZoneStep = new AddZoneStep();

    pipeline.WithRhinoObjectDeletion(idsToDelete)
        .WithCreationData(new CreationDataCollection<ZoneData>(zoneData))
        .WithUrbanElementStep(addZoneStep);

    return pipeline;
}
```

- [ ] **Step 3: Commit**

```bash
git add src/AIQ.Urban.RhinoInterface/Pipeline/RhinoUrbanModelPipeline.cs

git add src/AIQ.Urban.RhinoInterface/Pipeline/PipelineConfigurations.cs

git commit -m "refactor: pipeline resolves creation data from gather step or direct data"
```

---

## Task 4: Create Rhino Data Gather Steps

**Files:**
- Delete: `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDocActions/SelectCurveStep.cs`
- Create: `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDocActions/SelectTransitCenterlinesStep.cs`
- Create: `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDocActions/SelectBuildingFootprintsStep.cs`
- Create: `src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDocActions/SelectZoneBoundaryStep.cs`

**Interfaces:**
- Consumes: `RhinoDoc`, Rhino `Curve`, `Brep`, Rhino object selection APIs
- Produces: `CreationDataCollection<TransitData>`, `CreationDataCollection<BuildingData>`, `CreationDataCollection<ZoneData>` as `ICreationData`

- [ ] **Step 1: Delete `SelectCurveStep.cs`**

```bash
git rm src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDocActions/SelectCurveStep.cs
```

- [ ] **Step 2: Create `SelectTransitCenterlinesStep.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions;

internal class SelectTransitCenterlinesStep(
    IEnumerable<RhinoObject> selectedObjects,
    double width,
    TransitType transitType) : IRhinoDataGatherStep
{
    public ICreationData Run(RhinoDoc doc)
    {
        var selected = selectedObjects
            .Where(obj => obj.ObjectType == ObjectType.Curve)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more transit centerline curves first.");

        var transitData = new List<TransitData>();

        foreach (var obj in selected)
        {
            if (obj.Geometry is not Curve curve)
                continue;

            var curve2D = new Curve2D(curve, obj.Id);
            transitData.Add(new TransitData(curve2D, width, transitType));
        }

        if (transitData.Count == 0)
            throw new Exception("Selected objects must be curves.");

        return new CreationDataCollection<TransitData>(transitData);
    }
}
```

- [ ] **Step 3: Create `SelectBuildingFootprintsStep.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions;

internal class SelectBuildingFootprintsStep(
    IEnumerable<RhinoObject> selectedObjects,
    double floorHeight,
    int floorCount,
    Use use,
    string generationType) : IRhinoDataGatherStep
{
    public ICreationData Run(RhinoDoc doc)
    {
        if (generationType == "Mass")
            return GatherFromMass();

        return GatherFromFootprints();
    }

    private CreationDataCollection<BuildingData> GatherFromFootprints()
    {
        var selected = selectedObjects
            .Where(obj => obj.ObjectType == ObjectType.Curve)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more building footprint curves first.");

        var buildingData = new List<BuildingData>();

        foreach (var obj in selected)
        {
            if (obj.Geometry is not Curve curve)
                continue;

            var polygon = new Polygon2D(curve);
            buildingData.Add(new BuildingData(polygon, floorHeight, floorCount, use));
        }

        if (buildingData.Count == 0)
            throw new Exception("Selected objects must be curves.");

        return new CreationDataCollection<BuildingData>(buildingData);
    }

    private CreationDataCollection<BuildingData> GatherFromMass()
    {
        var selected = selectedObjects
            .Where(obj => obj.ObjectType == ObjectType.Brep)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more building envelope Breps first.");

        var buildingData = new List<BuildingData>();

        foreach (var obj in selected)
        {
            if (obj.Geometry is not Brep brep)
                continue;

            var footprint = ExtractFootprintFromBrep(brep);
            buildingData.Add(new BuildingData(footprint, floorHeight, floorCount, use));
        }

        if (buildingData.Count == 0)
            throw new Exception("Selected objects must be Breps.");

        return new CreationDataCollection<BuildingData>(buildingData);
    }

    private static Polygon2D ExtractFootprintFromBrep(Brep brep)
    {
        var bottomFace = brep.Faces
            .OrderBy(f => f.GetBoundingBox(true).Center.Z)
            .First();

        var curve = bottomFace.OuterLoop.To3dCurve();
        return new Polygon2D(curve);
    }
}
```

- [ ] **Step 4: Create `SelectZoneBoundaryStep.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions;

internal class SelectZoneBoundaryStep(
    IEnumerable<RhinoObject> selectedObjects,
    string name) : IRhinoDataGatherStep
{
    public ICreationData Run(RhinoDoc doc)
    {
        var selected = selectedObjects
            .Where(obj => obj.ObjectType == ObjectType.Curve)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select a zone boundary curve first.");

        var obj = selected.First();
        if (obj.Geometry is not Curve curve)
            throw new Exception("Selected object must be a curve.");

        var boundary = new Polygon2D(curve);
        return new CreationDataCollection<ZoneData>([new ZoneData(boundary, name)]);
    }
}
```

- [ ] **Step 5: Commit**

```bash
git add src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDocActions/SelectTransitCenterlinesStep.cs

git add src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDocActions/SelectBuildingFootprintsStep.cs

git add src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDocActions/SelectZoneBoundaryStep.cs

git add src/AIQ.Urban.RhinoInterface/Pipeline/RhinoDocActions/SelectCurveStep.cs

git commit -m "feat: add typed Rhino data gather steps for transit, building, zone"
```

---

## Task 5: Update Rhino Commands and HTML UI Messages

**Files:**
- Modify: `src/AIQ.Urban.Rhino/Commands/AddTransit_AIQUrban.cs`
- Modify: `src/AIQ.Urban.Rhino/Commands/AddBuilding_AIQUrban.cs`
- Modify: `src/AIQ.Urban.Rhino/Commands/AddZone_AIQUrban.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/TransitMessage.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/BuildingMessage.cs`

**Interfaces:**
- Consumes: `SelectTransitCenterlinesStep`, `SelectBuildingFootprintsStep`, `SelectZoneBoundaryStep`, `CreationDataCollection<T>`
- Produces: Commands and messages that delegate selection/data construction to the gather step

- [ ] **Step 1: Update `AddTransit_AIQUrban.cs`**

Add `using AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions;` and `using AIQ.Urban.Core.ModelActions.CreationData;`.

Replace the data construction and pipeline setup with:

```csharp
var centerlineRefs = go.Objects();
var rhinoObjs = centerlineRefs.Select(x => x.Object()).ToList();

var gatherStep = new SelectTransitCenterlinesStep(rhinoObjs, width, transitType);
var creationData = (CreationDataCollection<TransitData>)gatherStep.Run(doc);
var centerlineIds = creationData.Items.Select(t => t.Centerline.Id).ToList();

var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithDataGatherStep(gatherStep)
    .WithTransitAdditionSteps([], centerlineIds)
    .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name)
    .WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);

pipeline.Run();

var transitData = creationData.Items;
doc.Views.Redraw();
RhinoApp.WriteLine("Added {0} transit corridor(s), width={1}, type={2}.", transitData.Count, width, transitType);
```

Remove `BuildTransitData` and `TryValidateCenterline` helper methods.

- [ ] **Step 2: Update `AddBuilding_AIQUrban.cs`**

Add `using AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions;` and `using AIQ.Urban.Core.ModelActions.CreationData;`.

Replace the data construction and pipeline setup with:

```csharp
var selectedUse = DefaultUses.All[selectedUseIndex];
var floorHeight = floorHeightOption.CurrentValue;
var floorCount = floorCountOption.CurrentValue;
var curveRef = go.Object(0);
var rhinoObjs = new[] { curveRef.Object() };

var gatherStep = new SelectBuildingFootprintsStep(rhinoObjs, floorHeight, floorCount, selectedUse, "Footprint");
var creationData = (CreationDataCollection<BuildingData>)gatherStep.Run(doc);
var buildingData = creationData.Items;
var idsToDelete = buildingData.Select(b => b.Footprint.Id).ToList();

var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithDataGatherStep(gatherStep)
    .WithBuildingAdditionSteps([], idsToDelete)
    .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name)
    .WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);

pipeline.Run();

doc.Views.Redraw();
RhinoApp.WriteLine("Added building: {0} storeys, total height={1}, use={2}.",
    floorCount, floorHeight * floorCount, selectedUse.Name);
```

Remove the direct `footprint`/`Polygon2D`/`BuildingData` construction.

- [ ] **Step 3: Update `AddZone_AIQUrban.cs`**

Add `using AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions;` and `using AIQ.Urban.Core.ModelActions.CreationData;`.

Replace the data construction and pipeline setup with:

```csharp
var rhinoObjs = new[] { curveRef.Object() };

var gatherStep = new SelectZoneBoundaryStep(rhinoObjs, zoneName);
var creationData = (CreationDataCollection<ZoneData>)gatherStep.Run(doc);
var zoneData = creationData.Items;
var idsToDelete = zoneData.Select(z => z.Boundary.Id).ToList();

var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithDataGatherStep(gatherStep)
    .WithZoneAdditionSteps([], idsToDelete)
    .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name)
    .WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);

pipeline.Run();
```

- [ ] **Step 4: Update `TransitMessage.cs`**

Add `using AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions;` and `using AIQ.Urban.Core.ModelActions.CreationData;`.

Replace the body of `HandleMessage` with:

```csharp
var doc = RhinoDoc.ActiveDoc;
var selected = doc.Objects.GetSelectedObjects(false, false)
    .Where(obj => obj.ObjectType == ObjectType.Curve)
    .ToList();

if (selected.Count == 0)
    throw new Exception("Select one or more transit centerline curves first.");

var gatherStep = new SelectTransitCenterlinesStep(selected, RoadWidth, TransitType.Road);
var creationData = (CreationDataCollection<TransitData>)gatherStep.Run(doc);
var centerlineIds = creationData.Items.Select(t => t.Centerline.Id).ToList();

var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
    .WithDataGatherStep(gatherStep)
    .WithTransitAdditionSteps([], centerlineIds)
    .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

if (Urban_RhinoPlugin.Instance.EventHandler is { } handler)
    pipeline.WithEventHandler(handler);

pipeline.Run();
doc.Views.Redraw();

return new ModelStateMessage(UrbanModel.Instance);
```

- [ ] **Step 5: Update `BuildingMessage.cs`**

Add `using AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions;` and `using AIQ.Urban.Core.ModelActions.CreationData;`.

In `HandleFootprintMode`, replace the body after the data loop with:

```csharp
var gatherStep = new SelectBuildingFootprintsStep(selected, FloorHeight, FloorCount, use, "Footprint");
var creationData = (CreationDataCollection<BuildingData>)gatherStep.Run(doc);
var idsToDelete = creationData.Items.Select(b => b.Footprint.Id).ToList();

return RunPipeline(doc, gatherStep, idsToDelete);
```

In `HandleMassMode`, replace the body after the data loop with:

```csharp
var gatherStep = new SelectBuildingFootprintsStep(selected, FloorHeight, FloorCount, use, "Mass");
var creationData = (CreationDataCollection<BuildingData>)gatherStep.Run(doc);
var idsToDelete = creationData.Items.Select(b => b.Footprint.Id).ToList();

return RunPipeline(doc, gatherStep, idsToDelete);
```

Update `RunPipeline` signature:

```csharp
private ModelStateMessage RunPipeline(RhinoDoc doc, SelectBuildingFootprintsStep gatherStep, List<Guid> idsToDelete)
{
    var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
        .WithDataGatherStep(gatherStep)
        .WithBuildingAdditionSteps([], idsToDelete)
        .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

    if (Urban_RhinoPlugin.Instance.EventHandler is { } handler)
        pipeline.WithEventHandler(handler);

    pipeline.Run();
    doc.Views.Redraw();

    return new ModelStateMessage(UrbanModel.Instance);
}
```

- [ ] **Step 6: Commit**

```bash
git add src/AIQ.Urban.Rhino/Commands/AddTransit_AIQUrban.cs

git add src/AIQ.Urban.Rhino/Commands/AddBuilding_AIQUrban.cs

git add src/AIQ.Urban.Rhino/Commands/AddZone_AIQUrban.cs

git add src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/TransitMessage.cs

git add src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/BuildingMessage.cs

git commit -m "refactor: commands and UI messages use typed data gather steps"
```

---

## Task 6: Update Tests to Match New Step Signature

**Files:**
- Modify: `tests/AIQ.Urban.Core.Tests/ModelActions/AddBuildingStepTests.cs`
- Modify: `tests/AIQ.Urban.Core.Tests/ModelActions/AddTransitStepTests.cs`
- Modify: `tests/AIQ.Urban.Core.Tests/ModelActions/AddZoneStepTests.cs`
- Modify: `tests/AIQ.Urban.RhinoPlugin.Tests/ResetUrban_AIQUrbanTests.cs`

**Interfaces:**
- Consumes: `CreationDataCollection<T>`
- Produces: Compiling test calls to `step.Run(UrbanModel.Instance, ...)`

- [ ] **Step 1: Update `AddBuildingStepTests.cs`**

Change the arrange/act lines in `Run_AddsBuildingsToModel`:

```csharp
// Arrange
IUrbanModelStep step = new AddBuildingStep();

// Act
var result = step.Run(UrbanModel.Instance, new CreationDataCollection<BuildingData>(testData.Input));
```

- [ ] **Step 2: Update `AddTransitStepTests.cs`**

Change the arrange/act lines in `Run_AddsTransitsToModel`:

```csharp
// Arrange
IUrbanModelStep step = new AddTransitStep();

// Act
var result = step.Run(UrbanModel.Instance, new CreationDataCollection<TransitData>(testData.Input));
```

- [ ] **Step 3: Update `AddZoneStepTests.cs`**

Change the arrange/act lines in `Run_AddsZonesToModel`:

```csharp
// Arrange
IUrbanModelStep step = new AddZoneStep();

// Act
var result = step.Run(UrbanModel.Instance, new CreationDataCollection<ZoneData>(testData.Input));
```

- [ ] **Step 4: Update `ResetUrban_AIQUrbanTests.cs`**

Change the reset step usage:

```csharp
pipeline.WithUrbanElementStep(new ResetUrbanElementsStep());
pipeline.Run();
```

No change needed here because `ResetUrbanElementsStep` accepts `ICreationData?` and the pipeline resolves no creation data for a delete-only run. Verify this compiles.

- [ ] **Step 5: Commit**

```bash
git add tests/AIQ.Urban.Core.Tests/ModelActions/AddBuildingStepTests.cs

git add tests/AIQ.Urban.Core.Tests/ModelActions/AddTransitStepTests.cs

git add tests/AIQ.Urban.Core.Tests/ModelActions/AddZoneStepTests.cs

git add tests/AIQ.Urban.RhinoPlugin.Tests/ResetUrban_AIQUrbanTests.cs

git commit -m "test: update direct step tests for new ICreationData signature"
```

---

## Task 7: Build the Solution

**Files:**
- All of the above

**Interfaces:**
- N/A

- [ ] **Step 1: Run a full solution build**

```bash
dotnet build ArchSitePlan.sln -v q /clp:ErrorsOnly
```

Expected: Build succeeds with 0 errors.

- [ ] **Step 2: If any errors remain, fix them and rebuild**

Common likely follow-up fixes:
- Add missing `using AIQ.Urban.Core.ModelActions.CreationData;` where `CreationDataCollection<T>` is used.
- Remove unused `using` directives and dead helper methods in commands.
- Check any other tests that call `IUrbanModelStep.Run` directly.

- [ ] **Step 3: Commit final fixes**

```bash
git add -A

git commit -m "build: solution builds after creation data migration"
```

---

## Self-Review

**1. Spec coverage:**

| Spec Requirement | Implementing Task |
|--------------------|-------------------|
| `BuildingData`, `TransitData`, `ZoneData` implement `ICreationData` | Task 1 |
| `IRhinoDataGatherStep` gathers Rhino selections | Task 4 |
| `IAddUrbanElementsStep` receives creation data via pipeline | Tasks 2, 3 |
| `IDeleteUrbanElementsStep` ignores creation data | Task 2 |
| Solution builds | Task 7 |

**2. Placeholder scan:**

No `TBD`, `TODO`, or "implement later" strings. Every step contains the exact code to write.

**3. Type consistency:**

- `IUrbanModelStep.Run` signature is `Run(UrbanModel, ICreationData?)` everywhere.
- `CreationDataCollection<T>` is used consistently for building and zone.
- `TransitData`, `BuildingData`, `ZoneData` are consistently moved to `CreationData` namespace.

No gaps found.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-07-05-creation-data-gather-step-plan.md`. Two execution options:

1. **Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration.
2. **Inline Execution** — Execute tasks in this session using `executing-plans`, batch execution with checkpoints.

Which approach would you like?
