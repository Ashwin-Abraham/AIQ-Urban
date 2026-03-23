# Phase 1: Project Infrastructure

## Overview
Set up project structure, dependencies, and shared infrastructure.

## Changes Required

### 1. Update Project File
**File**: `AIQ.GH.Exports/AIQ.GH.Exports.csproj`

Add iText7 package reference for PDF merging:

```xml
<ItemGroup>
  <PackageReference Include="Grasshopper" Version="8.0.23304.9001" ExcludeAssets="runtime" />
  <PackageReference Include="iText7" Version="8.0.5" />
</ItemGroup>
```

### 2. Create Directory Structure
Create the following folders under `AIQ.GH.Exports/`:

```
Types/          # Custom GH data types
Helpers/        # RhinoCommon helpers
Components/     # One file per Grasshopper component
Resources/      # Icons (if needed)
```

### 3. Update Assembly Info
**File**: `AIQ.GH.Exports/AIQ.GH.ExportsInfo.cs`

Update with proper metadata:

```csharp
public override string Name => "AIQ.GH.Exports";
public override string Description => "Layout and PDF export components for Grasshopper";
public override string AuthorName => "AIQ";
public override string AuthorContact => "";
```

### 4. Remove Stub Component
Delete `AIQ.GH.Exports/GH_ExportsComponent.cs` - it will be replaced by organized components.

## Success Criteria

### Automated Verification
- [ ] Project builds without errors: `dotnet build AIQ.GH.Exports/AIQ.GH.Exports.csproj`
- [ ] NuGet packages restore successfully
- [ ] No compiler warnings for missing GUIDs or metadata
- [ ] Directory structure created correctly

## Dependencies Added
- `iText7` v8.0.5 - for PDF merging functionality
