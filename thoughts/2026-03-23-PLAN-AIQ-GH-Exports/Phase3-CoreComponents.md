# Phase 3: Core Grasshopper Components

## Overview
Implement the 5 Grasshopper components that form the core functionality of the plugin.

## Component Structure Pattern

All components follow this pattern:
```csharp
public class ComponentName : GH_Component
{
    public ComponentName() : base("Name", "Nick", "Description", "AIQ", "Subcategory") { }
    
    protected override void RegisterInputParams(GH_InputParamManager pManager) { }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }
    protected override void SolveInstance(IGH_DataAccess DA) { }
    public override Guid ComponentGuid => new Guid("...");
}
```

---

## 1. CopyLayoutView Component

**File**: `AIQ.GH.Exports/Components/CopyLayoutView.cs`

**Guid**: Generate new GUID

```
Category: AIQ
Subcategory: Layouts
```

### Inputs
| Name | Type | Access | Description |
|------|------|--------|-------------|
| Template Layout Name | String | Item | Name of existing layout to use as template |
| New Layout Name | String | Item | Name for the new layout |

### Outputs
| Name | Type | Description |
|------|------|-------------|
| Layout Name | String | Name of created layout |

### Key Implementation
```csharp
protected override void SolveInstance(IGH_DataAccess DA)
{
    string templateName = "";
    string newName = "";
    
    if (!DA.GetData(0, ref templateName)) return;
    if (!DA.GetData(1, ref newName)) return;
    
    var doc = RhinoDoc.ActiveDoc;
    var template = LayoutHelpers.GetLayoutByName(doc, templateName);
    
    if (template == null)
    {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Layout '{templateName}' not found");
        return;
    }
    
    var newLayout = LayoutHelpers.CreateLayoutFromTemplate(doc, templateName, newName);
    DA.SetData(0, newLayout.PageName);
}
```

---

## 2. SetDetailTarget Component

**File**: `AIQ.GH.Exports/Components/SetDetailTarget.cs`

**Guid**: Generate new GUID

```
Category: AIQ
Subcategory: Details
```

### Inputs
| Name | Type | Access | Description |
|------|------|--------|-------------|
| Layout Name | String | Item | Name of the layout containing the detail |
| Detail Name | String | Item | Name of the detail to modify |
| Target Point | Point | Item | World coordinates for detail's top-left corner |
| Scale | Number | Item | Scale ratio (e.g., 100 = 1:100) |

### Outputs
| Name | Type | Description |
|------|------|-------------|
| Success | Boolean | Whether the operation succeeded |

### Key Implementation
```csharp
protected override void SolveInstance(IGH_DataAccess DA)
{
    string layoutName = "", detailName = "";
    Point3d target = Point3d.Unset;
    double scale = 1.0;
    
    if (!DA.GetData(0, ref layoutName)) return;
    if (!DA.GetData(1, ref detailName)) return;
    if (!DA.GetData(2, ref target)) return;
    if (!DA.GetData(3, ref scale)) return;
    
    var doc = RhinoDoc.ActiveDoc;
    var layout = LayoutHelpers.GetLayoutByName(doc, layoutName);
    var detail = DetailHelpers.GetDetailByName(layout, detailName);
    
    if (detail == null)
    {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Detail '{detailName}' not found");
        DA.SetData(0, false);
        return;
    }
    
    DetailHelpers.SetDetailTarget(detail, target, scale, doc);
    DA.SetData(0, true);
}
```

---

## 3. CreateLayerConfiguration Component

**File**: `AIQ.GH.Exports/Components/CreateLayerConfiguration.cs`

**Guid**: Generate new GUID

```
Category: AIQ
Subcategory: Layers
```

### Inputs
| Name | Type | Access | Description |
|------|------|--------|-------------|
| Layer Names | String | List | Names of layers to configure |
| Visibility | Boolean | List | Per-layer visibility states |
| Display Colors | Color | List | Per-layer display colors (optional) |
| Print Colors | Color | List | Per-layer print colors (optional) |

### Outputs
| Name | Type | Description |
|------|------|-------------|
| Layer Config | LayerConfigurationGoo | Configuration for wire output |

### Key Implementation
```csharp
protected override void SolveInstance(IGH_DataAccess DA)
{
    List<string> names = new();
    List<bool> visibility = new();
    List<Color> displayColors = new();
    List<Color> printColors = new();
    
    if (!DA.GetDataList(0, names)) return;
    DA.GetDataList(1, visibility);
    DA.GetDataList(2, displayColors);
    DA.GetDataList(3, printColors);
    
    if (names.Count == 0)
    {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No layers specified");
        return;
    }
    
    var configs = new List<LayerConfiguration>();
    for (int i = 0; i < names.Count; i++)
    {
        var config = new LayerConfiguration(
            names[i],
            visibility.Count > i ? visibility[i] : true,
            displayColors.Count > i ? displayColors[i] : null,
            printColors.Count > i ? printColors[i] : null
        );
        configs.Add(config);
    }
    
    DA.SetData(0, new LayerConfigurationListGoo(configs));
}
```

---

## 4. PrintLayoutToPdf Component

**File**: `AIQ.GH.Exports/Components/PrintLayoutToPdf.cs`

**Guid**: Generate new GUID

```
Category: AIQ
Subcategory: Export
```

### Inputs
| Name | Type | Access | Description |
|------|------|--------|-------------|
| Layout Names | String | List | Names of layouts to export |
| Output Folder | String | Item | Destination folder for PDFs |
| Layer Config | LayerConfigurationGoo | Item | Layer settings to apply (optional) |

### Outputs
| Name | Type | Description |
|------|------|-------------|
| PDF Paths | String | Paths to created PDF files |
| Success | Boolean | Whether all exports succeeded |

### Key Implementation
```csharp
protected override void SolveInstance(IGH_DataAccess DA)
{
    List<string> layoutNames = new();
    string outputFolder = "";
    LayerConfigurationListGoo? layerConfigGoo = null;
    
    if (!DA.GetDataList(0, layoutNames)) return;
    if (!DA.GetData(1, ref outputFolder)) return;
    DA.GetData(2, ref layerConfigGoo);
    
    var doc = RhinoDoc.ActiveDoc;
    var layerConfigs = layerConfigGoo?.Value;
    
    var pdfPaths = new List<string>();
    bool allSuccess = true;
    
    foreach (var layoutName in layoutNames)
    {
        var layout = LayoutHelpers.GetLayoutByName(doc, layoutName);
        if (layout == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Layout '{layoutName}' not found, skipping");
            allSuccess = false;
            continue;
        }
        
        if (layerConfigs != null)
        {
            PdfHelpers.ApplyLayerConfigurations(doc, layerConfigs);
        }
        
        var pdfPath = PdfHelpers.ExportLayoutToPdf(layout, outputFolder, layerConfigs, doc);
        pdfPaths.Add(pdfPath);
    }
    
    DA.SetDataList(0, pdfPaths);
    DA.SetData(1, allSuccess);
}
```

---

## 5. MergePdfs Component

**File**: `AIQ.GH.Exports/Components/MergePdfs.cs`

**Guid**: Generate new GUID

```
Category: AIQ
Subcategory: Export
```

### Inputs
| Name | Type | Access | Description |
|------|------|--------|-------------|
| Input PDFs | String | List | Paths to PDF files to merge |
| Output Path | String | Item | Destination path for merged PDF |

### Outputs
| Name | Type | Description |
|------|------|-------------|
| Output Path | String | Path to merged PDF file |
| Success | Boolean | Whether merge succeeded |

### Key Implementation
```csharp
protected override void SolveInstance(IGH_DataAccess DA)
{
    List<string> inputPaths = new();
    string outputPath = "";
    
    if (!DA.GetDataList(0, inputPaths)) return;
    if (!DA.GetData(1, ref outputPath)) return;
    
    if (inputPaths.Count < 2)
    {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Need at least 2 PDFs to merge");
        DA.SetData(1, false);
        return;
    }
    
    try
    {
        PdfHelpers.MergePdfFiles(inputPaths, outputPath);
        DA.SetData(0, outputPath);
        DA.SetData(1, true);
    }
    catch (Exception ex)
    {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        DA.SetData(1, false);
    }
}
```

---

## Success Criteria

### Automated Verification
- [ ] All 5 components compile without errors
- [ ] Components appear in "AIQ" category with correct subcategories
- [ ] CopyLayoutView creates new layout with all details from template
- [ ] SetDetailTarget modifies detail viewport correctly
- [ ] CreateLayerConfiguration produces valid LayerConfigurationListGoo
- [ ] PrintLayoutToPdf exports to PDF files named after layouts
- [ ] MergePdfs combines PDFs in input order

### Manual Verification
- [ ] Components appear in Grasshopper component browser
- [ ] Component tooltips display correctly
- [ ] Error messages are helpful when invalid inputs provided
