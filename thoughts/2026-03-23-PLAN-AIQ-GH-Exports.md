# AIQ Grasshopper Exports Implementation Plan

## Overview

Create a Grasshopper plugin for Rhino that enables layout creation, detail management, and PDF export workflows. The plugin provides a series of components that transfer layout/drawing information via Grasshopper wires.

## Current State Analysis

The `AIQ.GH.Exports` project currently contains only stub files:
- `AIQ.GH.ExportsInfo.cs` - Empty assembly info
- `GH_ExportsComponent.cs` - Empty component template

No functionality exists. This is a greenfield implementation.

### Key Discoveries:
- Project targets `net7.0` and `net48` (Rhino 8 cross-platform) - `AIQ.GH.Exports.csproj:10`
- Package reference: Grasshopper `8.0.23304.9001`
- Assembly extension: `.gha` for Grasshopper loading
- Existing helper patterns in `ArchSitePlan.RhinoInterface\Helpers\` for document/view manipulation

## Desired End State

A functional Grasshopper plugin with 5 core components:
1. **Copy Layout View** - Creates layouts from templates (with details copied)
2. **Set Detail Target** - Configures detail view scale and camera target
3. **Create Layer Configuration** - Captures layer visibility/color settings
4. **Print Layout as PDF** - Exports layouts to individual PDFs
5. **Merge PDFs** - Combines multiple PDFs into one

All components communicate via custom GH data types passed through wires.

## What We're NOT Doing

- UI components for interactive layout editing
- Custom display modes
- Direct viewport manipulation (only through detail views)
- Support for Rhino 6/7 (targeting Rhino 8 only)
- Named view management (only layout/page views)
- Additional features like listing layouts, detail geometry controls, or layer state capture

## Implementation Phases

| Phase | Description | File |
|-------|-------------|------|
| [Phase 1: Project Infrastructure](2026-03-23-PLAN-AIQ-GH-Exports/Phase1-Infrastructure.md) | Update .csproj, create directory structure, update assembly info | Phase1-Infrastructure.md |
| [Phase 2: Data Types and Helpers](2026-03-23-PLAN-AIQ-GH-Exports/Phase2-DataTypesAndHelpers.md) | Custom GH data types and RhinoCommon helpers | Phase2-DataTypesAndHelpers.md |
| [Phase 3: Core Components](2026-03-23-PLAN-AIQ-GH-Exports/Phase3-CoreComponents.md) | 5 Grasshopper components | Phase3-CoreComponents.md |
| [Phase 4: Supporting Types](2026-03-23-PLAN-AIQ-GH-Exports/Phase4-SupportingTypes.md) | Additional GH wrapper types | Phase4-SupportingTypes.md |
| [Phase 5: Documentation](2026-03-23-PLAN-AIQ-GH-Exports/Phase5-Documentation.md) | Assembly info and final polish | Phase5-Documentation.md |

## File Structure Summary

```
AIQ.GH.Exports/
├── AIQ.GH.Exports.csproj              # Updated with iText7
├── AIQ.GH.ExportsInfo.cs               # Updated metadata
├── Types/
│   ├── LayerConfiguration.cs           # Data class
│   ├── LayerConfigurationGoo.cs        # GH wrapper
│   └── LayerConfigurationListGoo.cs   # List wrapper
├── Helpers/
│   ├── LayoutHelpers.cs                # Layout operations
│   ├── DetailHelpers.cs                # Detail operations
│   └── PdfHelpers.cs                   # PDF operations
└── Components/
    ├── CopyLayoutView.cs               # GH Component
    ├── SetDetailTarget.cs              # GH Component
    ├── CreateLayerConfiguration.cs     # GH Component
    ├── PrintLayoutToPdf.cs             # GH Component
    └── MergePdfs.cs                    # GH Component
```

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Grasshopper | 8.0.23304.9001 | GH SDK |
| iText7 | 8.0.5 | PDF merging |
| RhinoCommon | (bundled) | Layout/document operations |

## References

- Grasshopper SDK: https://developer.rhino3d.com/api/grasshopper/
- RhinoCommon Layout APIs: `RhinoPageView`, `DetailViewObject`
- iText7 Documentation: https://itextpdf.com/itext7
- Existing patterns: `ArchSitePlan.RhinoInterface\Helpers\ViewHelpers.cs`
