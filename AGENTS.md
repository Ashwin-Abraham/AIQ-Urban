# AGENTS.md — ArchSitePlan Development Guide

## Project Overview
ArchSitePlan is a .NET 8 solution (C# 12) for architectural site planning, integrating OSM mapping data, BHoM geometry, and Rhino/Grasshopper interfaces. It uses xUnit for testing and targets both Windows and cross-platform .NET.

## Build Commands

### Build Solution
```bash
dotnet build ArchSitePlan.sln
```

### Build Specific Configuration
```bash
# Default (Debug)
dotnet build ArchSitePlan.sln -c Debug
# Release
dotnet build ArchSitePlan.sln -c Release
# Without visualization dependencies (faster)
dotnet build ArchSitePlan.sln -c Debug_NoViz
# With visualization
dotnet build ArchSitePlan.sln -c DEBUG_Viz
```

### Run All Tests
```bash
dotnet test
# With verbose output
dotnet test -v n
```

### Run Tests for Specific Project
```bash
dotnet test AIQ.Primitives.Tests/AIQ.Primitives.Tests.csproj
dotnet test ArchSitePlan.Test/ArchSitePlan.Test.csproj
```

### Run a Single Test
```bash
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"
# Example
dotnet test --filter "FullyQualifiedName~DoubleExtensionTests.ToFormattedTextTests"
```

### Run Tests by Trait
```bash
dotnet test --filter "Category=Unit"
```

### Build Specific Project
```bash
dotnet build AIQ.Primitives/AIQ.Primitives.csproj
```

## Project Structure
```
ArchSitePlan/
├── AIQ.Primitives/              # Standalone utility library (no external deps)
├── AIQ.Primitives.Tests/
├── AIQ.Exports/                  # PowerPoint/Export functionality
├── AIQ.Exports.Tests/
├── AIQ.GH.Exports/               # Grasshopper plugin (net7.0 + net48)
├── AIQ.Mapbox/
├── AIQ.Mapbox.Tests/
├── AQI.Urban.Tests/              # Urban planning algorithm tests
├── ArchSitePlan.Core/            # Core domain models and logic
├── ArchSitePlan.BHoM.Geometry/   # BHoM geometry integration
├── ArchSitePlan.DebugModel/      # Debug visualization helpers
├── ArchSitePlan.Licensing/
├── ArchSitePlan.OSM/             # OpenStreetMap data parsing
├── ArchSitePlan.RhinoAddin/      # Rhino plugin entry point
├── ArchSitePlan.RhinoBhomInterop/ # BHoM ↔ Rhino interop
├── ArchSitePlan.RhinoInterface/   # Rhino interface layer
└── ArchSitePlan.Test/            # Main integration test project
```

## Code Style Guidelines

### File Organization
- **Namespace**: File-scoped namespaces (`namespace X.Y.Z;`)
- **Class placement**: One class per file; filename matches class name
- **Extension methods**: Place in `Extensions/` subfolder, file named `XxxExtensions.cs`
- **Helper/utility classes**: Place in `Helpers/` subfolder

### Naming Conventions
- Classes/Interfaces: `PascalCase` (e.g., `OsmModel`, `IOsmObject`)
- Methods: `PascalCase` (e.g., `FromOsmSource`)
- Private fields: `_camelCase` (e.g., `_config`, `_listOfAllUses`)
- Parameters: `camelCase` (e.g., `tolerance`, `comparison`)
- Constants: `PascalCase` with descriptive names (e.g., `MaxUseCountInTemplatePPT`)
- Test classes: `XxxTests` suffix (e.g., `DoubleExtensionTests`)
- Test methods: Descriptive, match what they test (e.g., `ToFormattedTextTests`)

### Code Patterns

#### Primary Constructors (C# 12)
```csharp
public class OsmModel(
    List<TransitOsm> highways,
    List<BuildingOsm> buildings)
{
    public List<TransitOsm> Highways { get; set; } = highways;
    public List<BuildingOsm> Buildings { get; set; } = buildings;
}
```

#### Collection Expressions
```csharp
List<int> slideNos = [];
var repeatingSlides = [2, 3, 4];
return [
    new Action1(),
    new Action2()
];
```

#### Pattern Matching
```csharp
var parcels = closedWays
    .Select(x => LandParcelOsm.FromOsmWay(x, nodes) as LandParcelOsm)
    .Where(x => x is not null)
    .Cast<LandParcelOsm>()
    .ToList();
```

#### LINQ Chaining
```csharp
return Highways.Cast<IOsmObject>()
    .Concat(LandParcels)
    .Concat(Buildings)
    .ToList();
```

### Formatting
- **Indentation**: 4 spaces (standard Visual Studio default)
- **Braces**: K&R style (opening brace on same line)
- **Line length**: No hard limit, but prefer readability
- **Blank lines**: Single blank line between methods, double between regions/groups

### Language Features
- **Implicit usings**: Enabled — do not add redundant `using System;` etc.
- **Nullable reference types**: Enabled — use `?` appropriately, avoid `!`
- **Target-typed new**: Prefer `new()` where type is clear
- **Switch expressions**: Use for simple mappings
- **Records**: Use `record` for immutable DTOs when appropriate

## Testing Conventions

### Test Framework
- **xUnit** with `[Fact]`, `[Theory]`, and `[InlineData]`
- **Microsoft.NET.Test.Sdk** for test discovery
- **coverlet.collector** for code coverage

### Test Structure
```csharp
[Fact]
public void MethodName_ExpectedBehavior()
{
    // Arrange
    var input = 123.45;
    
    // Act
    var result = input.ToFormattedText();
    
    // Assert
    Assert.Equal("123.45", result);
}
```

### Theory Tests
```csharp
[Theory]
[InlineData(345.67, "345.67")]
[InlineData(123345.67, "123,345.67")]
public void ToFormattedTextTests(double number, string expected, bool withCommas = true)
{
    var result = number.ToFormattedText(withCommas);
    Assert.Equal(expected, result);
}
```

### Debug-Only Test Code
```csharp
#if DEBUG
var roadCLs = model.Highways.Select(x => x.Centerline).ToList();
roadCLs.ToElements().Visualize();
#endif
```

### Global Usings for Tests
```csharp
// In GlobalUsings.cs (with DEBUG guard when needed)
#if DEBUG
global using ArchSitePlan.DebugModel;
#endif

// In test project .csproj
<ItemGroup>
    <Using Include="Xunit" />
</ItemGroup>
```

## Project Configuration

### SDK Style Projects
All projects use `Microsoft.NET.Sdk` — no legacy `.csproj` format.

### Key Properties
```xml
<TargetFramework>net8.0</TargetFramework>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
<LangVersion>12.0</LangVersion>
```

### Test Project Properties
```xml
<IsPackable>false</IsPackable>
<IsTestProject>true</IsTestProject>
```

## Error Handling
- Use `try-catch` for expected exceptions with specific handling
- Let unexpected exceptions propagate; don't silently swallow
- Return `null` or `false` for recoverable failures (see `TryMakeOSMModelFromFile`)
- Use `ArgumentNullException.ThrowIfNull()` for guard clauses

## Dependencies
- **BHoM** (Buildings and Habitats object model) for geometry
- **RhinoCommon** for 3D visualization (Windows-only)
- **Grasshopper** for parametric modeling
- **OsmSharp** for OpenStreetMap data parsing
- **xunit** for testing

## Notes
- Projects marked with `Debug_NoViz` configuration skip visualization-heavy dependencies for faster builds
- `AIQ.GH.Exports` targets both `net7.0` and `net48` for Grasshopper compatibility
- Debug visualization helpers are only available in DEBUG builds
