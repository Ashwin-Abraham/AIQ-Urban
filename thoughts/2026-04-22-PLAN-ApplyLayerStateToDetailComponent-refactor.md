# ApplyLayerStateToDetailComponent Refactor Implementation Plan

## Overview

Refactor `ApplyLayerStateToDetailComponent` to accept a `DocLayerState` Goo instead of a text layer state name. The component should apply the layer properties from the `DocLayerState` to the detail's viewport using RhinoCommon's `Layer.SetPersistent*` methods for per-viewport layer settings.

## Current State Analysis

**Current Implementation:**
- `ApplyLayerStateToDetailComponent.cs` (lines 14-28): Accepts text input for layer state name
- `DetailExtensions.ApplyLayerStateToDetail()` (lines 10-23): Uses `doc.NamedLayerStates.Restore()` with `RestoreLayerProperties.ViewportVisible` and `ViewportColor` to apply per-viewport settings

**Existing Types:**
- `DocLayerState` (`Models/DocLayerState.cs`): Contains `List<LayerProperties>` with FullPath, LayerOn, LayerDisplayColor, LayerLineWeight, LayerLineType, LayerPlotColor, LayerPlotWeight
- `DocLayerStateGoo` (`Types/DocLayerStateGoo.cs`): Grasshopper goo wrapper for DocLayerState

**Key Files:**
- `AIQ.GH.Exports/Components/ApplyLayerStateToDetailComponent.cs`
- `AIQ.GH.Exports/Extensions/DetailExtensions.cs`
- `AIQ.GH.Exports/Models/DocLayerState.cs`
- `AIQ.GH.Exports/Models/LayerProperties.cs`
- `AIQ.GH.Exports/Types/DocLayerStateGoo.cs`

## Desired End State

1. Component input changed from text to `DocLayerStateGoo`
2. New method in `DetailExtensions` to apply `DocLayerState` properties to detail viewport using `Layer.SetPersistent*` methods
3. All applicable layer properties applied per-viewport:
   - Visibility (`SetPersistentVisibility`)
   - Display Color (`SetPersistentDisplayColor`)
   - Plot Color (`SetPersistentPlotColor`)
   - Plot Weight (`SetPersistentPlotWeight`)
   - Linetype (`SetPersistentLinetype`)

### Key Discoveries:
- Current method only applies `ViewportVisible` and `ViewportColor` via Rhino's named layer state system
- RhinoCommon `Layer` class has per-viewport persistence methods that can be used directly with viewport IDs
- `DetailViewObject.Viewport.Id` provides the viewport identifier needed for per-viewport settings

## What We're NOT Doing

- Not changing the output signature (still returns Success boolean)
- Not supporting undo/redo (Rhino's named layer states have this built-in, direct method won't)
- Not modifying other components that use DocLayerState

## Implementation Approach

1. Modify component input parameter from text to `DocLayerStateGoo`
2. Create new extension method in `DetailExtensions` that iterates through `LayerProperties` and applies each to the detail's viewport using `Layer.SetPersistent*` methods
3. The new method will find each layer by FullPath, then apply viewport-specific properties

## Phase 1: Add ApplyDocLayerStateToDetail Extension Method

### Overview
Create a new method in `DetailExtensions.cs` that applies DocLayerState properties to a detail viewport using RhinoCommon's SetPersistent* methods.

### Changes Required:

#### 1. DetailExtensions.cs
**File**: `AIQ.GH.Exports/Extensions/DetailExtensions.cs`
**Changes**: Add new method to apply DocLayerState to detail

```csharp
// New method to add
public static bool ApplyDocLayerStateToDetail(
    this DetailViewObject detail,
    DocLayerState docLayerState,
    RhinoDoc doc)
{
    if (docLayerState?.LayerState is null || detail is null)
        return false;

    var viewportId = detail.Viewport.Id;
    bool success = true;

    foreach (var props in docLayerState.LayerState)
    {
        var layerIndex = doc.Layers.FindByFullPath(props.FullPath, -1);
        if (layerIndex < 0)
            continue;

        var layer = doc.Layers[layerIndex];

        if (props.LayerOn.HasValue)
        {
            layer.SetPersistentVisibility(viewportId, props.LayerOn.Value);
        }

        if (props.LayerDisplayColor.HasValue)
        {
            layer.SetPersistentDisplayColor(viewportId, props.LayerDisplayColor.Value);
        }

        if (props.LayerPlotColor.HasValue)
        {
            layer.SetPersistentPlotColor(viewportId, props.LayerPlotColor.Value);
        }

        if (props.LayerPlotWeight.HasValue)
        {
            layer.SetPersistentPlotWeight(viewportId, props.LayerPlotWeight.Value);
        }

        if (!string.IsNullOrEmpty(props.LayerLineType))
        {
            var lineTypeIndex = doc.Linetypes.Find(props.LayerLineType, true);
            if (lineTypeIndex >= 0)
            {
                layer.SetPersistentLinetype(viewportId, lineTypeIndex);
            }
        }
    }

    if (success)
    {
        detail.CommitViewportChanges();
        detail.CommitChanges();
    }

    return success;
}
```

### Success Criteria:

#### Automated Verification:
- [x] Solution builds: `dotnet build ArchSitePlan.sln`
- [x] Unit tests pass: `dotnet test`

#### Manual Verification:
- [ ] Component appears in Grasshopper with new input type
- [ ] Applying DocLayerState to detail correctly sets per-viewport layer visibility
- [ ] Applying DocLayerState to detail correctly sets per-viewport layer colors

---

## Phase 2: Update ApplyLayerStateToDetailComponent

### Overview
Modify the component to accept DocLayerStateGoo instead of text input.

### Changes Required:

#### 1. ApplyLayerStateToDetailComponent.cs
**File**: `AIQ.GH.Exports/Components/ApplyLayerStateToDetailComponent.cs`
**Changes**: Update input parameter registration and SolveInstance logic

- Change input param from `AddTextParameter` to `AddGenericParameter` with `DocLayerStateGoo`
- Update `SolveInstance` to:
  - Get `DocLayerStateGoo` input instead of text
  - Call new extension method instead of using named layer states
- Add required using statement for `DocLayerStateGoo`

```csharp
// RegisterInputParams changes:
pManager.AddGenericParameter("Doc Layer State", "DL", "DocLayerState to apply", GH_ParamAccess.item);

// SolveInstance changes:
DocLayerStateGoo? goo = null;
if (!DA.GetData(0, ref goo) || goo?.Value is null)
{
    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid DocLayerState input");
    DA.SetData(0, false);
    return;
}

bool success = detail?.ApplyDocLayerStateToDetail(goo.Value, doc) ?? false;
```

### Success Criteria:

#### Automated Verification:
- [x] Solution builds: `dotnet build ArchSitePlan.sln`
- [x] Unit tests pass: `dotnet test`

#### Manual Verification:
- [ ] Component accepts DocLayerStateGoo input
- [ ] Layer state properties are correctly applied to detail viewport

---

## Testing Strategy

### Unit Tests:
- Test that new extension method handles null inputs gracefully
- Test that it correctly finds layers by FullPath
- Test that it handles missing layers gracefully

### Manual Testing Steps:
1. Create a DocLayerState using existing components
2. Add ApplyLayerStateToDetailComponent to Grasshopper canvas
3. Connect DocLayerState to new input
4. Connect a Detail to the Detail input
5. Verify layer visibility, color, plot weight, and linetype are applied per-viewport in Rhino

## Performance Considerations

- The new method iterates through all LayerProperties in the DocLayerState
- For large layer states, this could be slow; consider adding optional filter parameter if performance is an issue

## Migration Notes

This is a breaking change for existing Grasshopper definitions:
- Old: Text input for layer state name
- New: DocLayerStateGoo input

Users will need to update their existing definitions.

## References

- Original component: `AIQ.GH.Exports/Components/ApplyLayerStateToDetailComponent.cs`
- Existing extension: `AIQ.GH.Exports/Extensions/DetailExtensions.cs`
- DocLayerState model: `AIQ.GH.Exports/Models/DocLayerState.cs`
- LayerProperties model: `AIQ.GH.Exports/Models/LayerProperties.cs`
