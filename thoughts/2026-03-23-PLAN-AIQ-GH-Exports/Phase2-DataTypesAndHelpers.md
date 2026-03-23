# Phase 2: Data Types and Helpers

## Overview
Create custom GH_Goo wrapper for layer configurations and helper classes for Rhino operations.

## Changes Required

### 1. LayerConfiguration Data Class
**File**: `AIQ.GH.Exports/Types/LayerConfiguration.cs`

Represents a single layer's configuration (visibility, colors, lock state).

```csharp
namespace AIQ.GH.Exports.Types;

public class LayerConfiguration
{
    public string LayerName { get; }
    public bool IsVisible { get; }
    public Color DisplayColor { get; }
    public Color PrintColor { get; }
    public bool IsLocked { get; }
    
    public LayerConfiguration(
        string layerName,
        bool isVisible = true,
        Color? displayColor = null,
        Color? printColor = null,
        bool isLocked = false);
    
    public static LayerConfiguration FromRhinoLayer(Layer layer);
    public void ApplyToRhinoLayer(Layer layer);
    public string ToJson();
    public static LayerConfiguration FromJson(string json);
}
```

### 2. LayerConfigurationGoo Wrapper
**File**: `AIQ.GH.Exports/Types/LayerConfigurationGoo.cs`

GH_Goo wrapper for serializing/deserializing LayerConfiguration through GH wires.

```csharp
public class LayerConfigurationGoo : GH_Goo<LayerConfiguration>
{
    public override bool IsValid => Value != null;
    public override string IsValidWhyNot => Value == null ? "LayerConfiguration is null" : "";
    
    public override IGH_Goo Duplicate() 
        => new LayerConfigurationGoo { Value = Value };
    
    public override string ToString() 
        => Value?.LayerName ?? "null";
    
    public override bool Write(GH_IWriter writer) 
        => writer.SetString("data", Value?.ToJson());
    
    public override bool Read(GH_IReader reader)
    {
        var json = reader.GetString("data");
        if (string.IsNullOrEmpty(json))
        {
            Value = null;
            return true;
        }
        Value = LayerConfiguration.FromJson(json);
        return true;
    }
}
```

### 3. LayoutHelpers
**File**: `AIQ.GH.Exports/Helpers/LayoutHelpers.cs`

RhinoCommon helpers for layout operations.

```csharp
public static class LayoutHelpers
{
    public static RhinoPageView CreateLayoutFromTemplate(
        RhinoDoc doc,
        string templateLayoutName,
        string newLayoutName);
    
    public static IEnumerable<string> GetAllLayoutNames(RhinoDoc doc);
    
    public static RhinoPageView? GetLayoutByName(RhinoDoc doc, string name);
    
    public static void CopyDetailsToLayout(
        RhinoPageView source,
        RhinoPageView destination);
    
    public static RhinoPageView CreateEmptyLayout(
        RhinoDoc doc,
        string layoutName,
        double width,
        double height,
        UnitSystem units = UnitSystem.Millimeters);
}
```

### 4. DetailHelpers
**File**: `AIQ.GH.Exports/Helpers/DetailHelpers.cs`

RhinoCommon helpers for detail view operations.

```csharp
public static class DetailHelpers
{
    public static DetailViewObject? GetDetailByName(
        RhinoPageView layout,
        string detailName);
    
    public static List<DetailViewObject> GetAllDetails(RhinoPageView layout);
    
    public static void SetDetailTarget(
        DetailViewObject detail,
        Point3d worldTarget,
        double modelScaleRatio,
        RhinoDoc doc);
    
    public static void SetDetailScale(
        DetailViewObject detail,
        double modelUnitsPerPageUnit,
        RhinoDoc doc);
    
    public static void SetDetailProjection(
        DetailViewObject detail,
        DefinedViewportProjection projection,
        RhinoDoc doc);
    
    public static void UpdateDetailGeometry(
        DetailViewObject detail,
        Point2d topLeft,
        Point2d bottomRight,
        RhinoDoc doc);
}
```

### 5. PdfHelpers
**File**: `AIQ.GH.Exports/Helpers/PdfHelpers.cs`

Helpers for PDF export and merging.

```csharp
public static class PdfHelpers
{
    public static string ExportLayoutToPdf(
        RhinoPageView layout,
        string outputFolder,
        IEnumerable<LayerConfiguration>? layerConfigs,
        RhinoDoc doc);
    
    public static void ApplyLayerConfigurations(
        RhinoDoc doc,
        IEnumerable<LayerConfiguration> configs);
    
    public static void MergePdfFiles(
        IEnumerable<string> inputPaths,
        string outputPath);
}
```

## Success Criteria

### Automated Verification
- [ ] LayerConfiguration can be serialized/deserialized via GH_Goo
- [ ] LayoutHelpers creates layouts with all details copied from template
- [ ] DetailHelpers correctly sets scale using `SetScale()` API
- [ ] PdfHelpers exports single layout to PDF with layer visibility applied
- [ ] All helper methods handle null/edge cases gracefully
- [ ] Unit tests pass for serialization edge cases

## Key API References

| Class | Method | Notes |
|-------|--------|-------|
| `RhinoPageView` | `doc.Views.AddPageView()` | Creates new layout |
| `DetailViewObject` | `pageView.AddDetailView()` | Adds detail to layout |
| `DetailViewObject` | `detail.DetailGeometry.SetScale()` | Sets detail scale |
| `RhinoViewport` | `SetCameraTarget()` | Sets viewport target |
| `FilePdf` | `FilePdf.Create()` | Creates PDF document |
| `PdfMerger` | iText7 class | Merges multiple PDFs |
