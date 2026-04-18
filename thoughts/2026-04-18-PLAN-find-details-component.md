# Find Details Component Implementation Plan

## Overview

Create a Grasshopper component that finds one or more details in Rhino layouts using configurable text matching strategies. The component searches across layouts and details based on user-provided criteria.

## Current State Analysis

- **Existing Components**: `SetDetailLayerStateComponent` at `AIQ.GH.Exports/Components/SetDetailLayerStateComponent.cs:14` provides similar layout/detail search functionality
- **Existing Helpers**: 
  - `LayoutHelpers.GetLayoutByName` and `GetAllLayoutNames` in `LayoutHelpers.cs:73-86`
  - `DetailHelpers.GetDetailByName` and `GetAllDetails` in `DetailHelpers.cs:16-27`
- **No existing text matching enum**: Need to create `TextMatchStrategy` enum and helper class

## Desired End State

A new Grasshopper component that:
- Takes Layout Name, Detail Name, and Text Match Strategy as inputs
- Returns a list of `DetailViewObject` matching the criteria
- Uses `GH_PersistentGeometry` to output native `DetailViewObject` objects (not goo)

### Key Discoveries:
- Component should output `List<DetailViewObject>` via `GH_PersistentGeometry<DetailViewObject>` per `SetDetailLayerStateComponent.cs:28`
- Text matching should be in a separate helpers class per user request

## What We're NOT Doing

- Creating goo wrappers for DetailViewObject
- Adding any UI/visual modifications to details
- Implementing pagination for large result sets (return all matches)

## Implementation Approach

1. Create `TextMatchStrategy` enum in `AIQ.GH.Exports/Enums/`
2. Create `TextMatchHelpers` class in `AIQ.GH.Exports/Helpers/`
3. Create `FindDetailsComponent` in `AIQ.GH.Exports/Components/`
4. Add component GUID to `ComponentGuids.cs`

---

## Phase 1: Create TextMatchStrategy Enum

### Overview
Create an enum to define text matching strategies.

### Changes Required:

#### 1. TextMatchStrategy Enum
**File**: `AIQ.GH.Exports/Enums/TextMatchStrategy.cs`
**Changes**: Create new enum with values for text matching

```csharp
namespace AIQ.GH.Exports.Enums;

public enum TextMatchStrategy
{
    Contains,
    DoesNotContain,
    ExactMatch
}
```

### Success Criteria:

#### Automated Verification:
- [x] Build succeeds: `dotnet build AIQ.GH.Exports/AIQ.GH.Exports.csproj`

#### Manual Verification:
- [x] Enum is accessible from component code

---

## Phase 2: Create TextMatchHelpers Class

### Overview
Create a helper class with text matching logic for layouts and details.

### Changes Required:

#### 1. TextMatchHelpers
**File**: `AIQ.GH.Exports/Helpers/TextMatchHelpers.cs`
**Changes**: Add text matching methods using strategy

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.GH.Exports.Enums;
using Rhino;
using Rhino.Display;

namespace AIQ.GH.Exports.Helpers;

public static class TextMatchHelpers
{
    public static bool Matches(string? text, string? pattern, TextMatchStrategy strategy)
    {
        if (string.IsNullOrEmpty(pattern))
            return true;
        
        if (text is null)
            return false;

        return strategy switch
        {
            TextMatchStrategy.Contains => text.Contains(pattern, StringComparison.OrdinalIgnoreCase),
            TextMatchStrategy.DoesNotContain => !text.Contains(pattern, StringComparison.OrdinalIgnoreCase),
            TextMatchStrategy.ExactMatch => text.Equals(pattern, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    public static List<RhinoPageView> FindLayouts(RhinoDoc doc, string? layoutName, TextMatchStrategy strategy)
    {
        var allLayouts = doc.Views
            .Where(v => v is RhinoPageView)
            .Cast<RhinoPageView>()
            .ToList();

        if (string.IsNullOrEmpty(layoutName))
            return allLayouts;

        return allLayouts
            .Where(l => Matches(l.PageName, layoutName, strategy))
            .ToList();
    }

    public static List<DetailViewObject> FindDetails(
        List<RhinoPageView> layouts,
        string? detailName,
        TextMatchStrategy strategy)
    {
        var allDetails = layouts
            .SelectMany(l => l.GetDetailViews())
            .ToList();

        if (string.IsNullOrEmpty(detailName))
            return allDetails;

        return allDetails
            .Where(d => Matches(d.Name, detailName, strategy))
            .ToList();
    }
}
```

### Success Criteria:

#### Automated Verification:
- [x] Build succeeds: `dotnet build AIQ.GH.Exports/AIQ.GH.Exports.csproj`

#### Manual Verification:
- [x] Methods are accessible and work correctly

---

## Phase 3: Create FindDetailsComponent

### Overview
Create the main Grasshopper component.

### Changes Required:

#### 1. Component GUID
**File**: `AIQ.GH.Exports/Components/ComponentGuids.cs`
**Changes**: Add new GUID for FindDetails component

```csharp
public static System.Guid FindDetails => new("...");
```

#### 2. FindDetailsComponent
**File**: `AIQ.GH.Exports/Components/FindDetailsComponent.cs`
**Changes**: Create new component

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.GH.Exports.Enums;
using AIQ.GH.Exports.Helpers;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Display;

namespace AIQ.GH.Exports.Components;

public class FindDetailsComponent : GH_Component
{
    public FindDetailsComponent()
        : base("Find Details", "FindDetails",
               "Finds details in layouts by name using text matching strategies",
               "AIQ", "Layout")
    { }

    public override Guid ComponentGuid => ComponentGuids.FindDetails;

    protected override Bitmap Icon => IconLoader.LoadIcon("Find Details.png");

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
        manager.AddTextParameter("Layout Name", "L", "Layout to search in. If empty, searches all layouts", GH_ParamAccess.item);
        manager.AddTextParameter("Detail Name", "D", "Detail name to search for. If empty, returns all details in matching layouts", GH_ParamAccess.item);
        manager.AddGenericParameter("Text Match Strategy", "M", "Text matching strategy: Contains, DoesNotContain, ExactMatch", GH_ParamAccess.item);

        manager[0].Optional = true;
        manager[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
        manager.AddGenericParameter("Details", "D", "List of matching detail objects", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        var doc = RhinoDoc.ActiveDoc;
        if (doc is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document");
            return;
        }

        string? layoutName = null;
        da.GetData(0, ref layoutName);

        string? detailName = null;
        da.GetData(1, ref detailName);

        var strategy = TextMatchStrategy.Contains;
        if (!da.GetData(2, ref strategy))
        {
            strategy = TextMatchStrategy.Contains;
        }

        var layouts = TextMatchHelpers.FindLayouts(doc, layoutName, strategy);
        
        if (layouts.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No layouts found matching criteria");
            da.SetDataList(0, new List<DetailViewObject>());
            return;
        }

        var details = TextMatchHelpers.FindDetails(layouts, detailName, strategy);

        if (details.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No details found matching criteria");
        }

        da.SetDataList(0, details);
    }
}
```

### Success Criteria:

#### Automated Verification:
- [x] Build succeeds: `dotnet build AIQ.GH.Exports/AIQ.GH.Exports.csproj`

#### Manual Verification:
- [ ] Component appears in Grasshopper UI under AIQ > Layout
- [ ] Component functions correctly with all input combinations

---

## Testing Strategy

### Unit Tests:
- Test `TextMatchHelpers.Matches` with each strategy
- Test empty/null inputs handling

### Manual Testing Steps:
1. Create a test layout with multiple details in Rhino
2. Connect component with various input combinations:
   - Empty layout name + empty detail name → should return all details
   - Specific layout + empty detail → should return all details in that layout
   - Specific layout + specific detail + Contains → should find matching details
   - Specific layout + specific detail + ExactMatch → should match exactly
   - Specific layout + specific detail + DoesNotContain → should filter out matching
3. Verify output is a list of DetailViewObject that can be connected to other components

## Performance Considerations

- Uses LINQ for efficient filtering
- No caching needed - searches are fast for typical document sizes

## References

- Original feature request from user
- Similar component: `SetDetailLayerStateComponent.cs:14`
- Helper classes: `LayoutHelpers.cs`, `DetailHelpers.cs`