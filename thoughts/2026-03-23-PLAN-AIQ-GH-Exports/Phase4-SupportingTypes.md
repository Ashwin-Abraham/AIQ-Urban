# Phase 4: Supporting Types

## Overview
Create additional supporting types for robust component wiring and data passing between components.

## Changes Required

### 1. LayerConfigurationListGoo
**File**: `AIQ.GH.Exports/Types/LayerConfigurationListGoo.cs`

GH_Goo wrapper for `List<LayerConfiguration>` to pass multiple layer configurations through a single wire.

```csharp
public class LayerConfigurationListGoo : GH_Goo<List<LayerConfiguration>>
{
    public override bool IsValid => Value != null && Value.Count > 0;
    
    public override string IsValidWhyNot
    {
        get
        {
            if (Value == null) return "LayerConfigurationList is null";
            if (Value.Count == 0) return "LayerConfigurationList is empty";
            return "";
        }
    }
    
    public override IGH_Goo Duplicate()
    {
        var copy = new LayerConfigurationListGoo();
        if (Value != null)
        {
            copy.Value = Value.Select(c => new LayerConfiguration(
                c.LayerName,
                c.IsVisible,
                c.DisplayColor,
                c.PrintColor,
                c.IsLocked
            )).ToList();
        }
        return copy;
    }
    
    public override string ToString()
    {
        if (Value == null || Value.Count == 0) return "Empty";
        return $"{Value.Count} layers: {string.Join(", ", Value.Take(3).Select(v => v.LayerName))}"
            + (Value.Count > 3 ? "..." : "");
    }
    
    public override bool Write(GH_IWriter writer)
    {
        if (Value == null) return false;
        
        var sb = new StringBuilder();
        foreach (var config in Value)
        {
            sb.AppendLine(config.ToJson());
        }
        return writer.SetString("data", sb.ToString());
    }
    
    public override bool Read(GH_IReader reader)
    {
        var data = reader.GetString("data");
        if (string.IsNullOrEmpty(data))
        {
            Value = null;
            return true;
        }
        
        Value = new List<LayerConfiguration>();
        foreach (var line in data.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var config = LayerConfiguration.FromJson(line.Trim());
            if (config != null)
            {
                Value.Add(config);
            }
        }
        return true;
    }
}
```

### 2. GH_LayerConfigurationParam (Optional)
**File**: `AIQ.GH.Exports/Types/LayerConfigurationParam.cs`

Custom parameter type for better UX when wiring layer configurations. This allows right-click "Set Single Layer" vs "Set Multiple Layers" behavior.

```csharp
public class GH_LayerConfigurationParam : GH_PersistentParam<LayerConfigurationGoo>
{
    public override Guid InstanceGuid => new Guid("...");
    public override string TypeName => "Layer Configuration";
    public override string TypeDescription => "A single layer configuration";
    
    protected override System.Drawing.Bitmap Icon => /* icon */;
    
    public override void AppendMenuItems(ToolStripDropDown menu)
    {
        base.AppendMenuItems(menu);
        
        // Add "Set multiple" option
        var item = Menu_AppendItem(menu, "Set multiple layers...");
        item.Click += (s, e) => /* show multi-select dialog */;
    }
}
```

**Note**: This is optional - `GH_GenericParam` with `LayerConfigurationGoo` works for basic functionality.

### 3. Component GUIDs Registry
**File**: `AIQ.GH.Exports/Components/ComponentGuids.cs`

Centralized GUID storage for all components.

```csharp
namespace AIQ.GH.Exports.Components;

public static class ComponentGuids
{
    public static readonly Guid CopyLayoutView = new Guid("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");
    public static readonly Guid SetDetailTarget = new Guid("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");
    public static readonly Guid CreateLayerConfiguration = new Guid("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");
    public static readonly Guid PrintLayoutToPdf = new Guid("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");
    public static readonly Guid MergePdfs = new Guid("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");
}
```

## Success Criteria

### Automated Verification
- [ ] LayerConfigurationListGoo serializes/deserializes correctly
- [ ] Multiple configurations can be passed through a single wire
- [ ] Component GUIDs are consistent across all components
- [ ] ListGoo displays correct count in tooltips

### Manual Verification
- [ ] Data flows correctly from CreateLayerConfiguration to PrintLayoutToPdf
- [ ] Multiple components can consume the same LayerConfigurationListGoo output
