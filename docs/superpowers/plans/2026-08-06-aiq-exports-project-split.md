# AIQ.Exports Project Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split Google Slides and PowerPoint code from the Excel export assembly so the Rhino plugin release dependency graph excludes the future export packages.

**Architecture:** `AIQ.Exports` remains the stable Excel assembly used by `AIQ.Urban.RhinoInterface`. A new independent `AIQ.Exports.Future` assembly owns Google Slides and PowerPoint code. Each assembly has a matching xUnit test project, and public namespaces stay unchanged.

**Tech Stack:** .NET 8, C# 12, SDK-style projects, xUnit, ClosedXML, Google APIs, ShapeCrawler, MSBuild, Graphify

## Global Constraints

- Keep all current `AIQ.Exports.*` namespaces and public member names unchanged.
- `AIQ.Exports` must contain only Excel export code and Excel resources.
- `AIQ.Exports.Future` must contain Google Slides and PowerPoint export code and related resources.
- `AIQ.Exports.Future` must not reference `AIQ.Exports`.
- `AIQ.Urban.RhinoInterface` must continue to reference only `AIQ.Exports`.
- Preserve package versions from the current `AIQ.Exports.csproj`.
- Preserve the user's uncommitted `AIQ.Urban.RhinoPlugin` version change to `3.0.0`.
- Use xUnit in both export test projects.

---

### Task 1: Split the production export assemblies

**Files:**

- Modify: `src/AIQ.Exports/AIQ.Exports.csproj`
- Create: `src/AIQ.Exports.Future/AIQ.Exports.Future.csproj`
- Move: `src/AIQ.Exports/GoogleSlides/**` to `src/AIQ.Exports.Future/GoogleSlides/**`
- Move: `src/AIQ.Exports/PowerpointExport/**` to `src/AIQ.Exports.Future/PowerpointExport/**`
- Move: `src/AIQ.Exports/AIQ Export Resources/AIQUrbanTemplate.pptx` to `src/AIQ.Exports.Future/AIQ Export Resources/AIQUrbanTemplate.pptx`
- Move: `src/AIQ.Exports/AIQ Export Resources/LEGO.png` to `src/AIQ.Exports.Future/AIQ Export Resources/LEGO.png`
- Keep: `src/AIQ.Exports/Excel/**`
- Keep: `src/AIQ.Exports/AIQ Export Resources/TemplateExcel.xlsx`

**Interfaces:**

- Consumes: existing public types in `AIQ.Exports.Excel.*`, `AIQ.Exports.GoogleSlides.*`, and `AIQ.Exports.PowerpointExport.*`
- Produces: independent assemblies `AIQ.Exports.dll` and `AIQ.Exports.Future.dll` with the same public namespaces as before

- [ ] **Step 1: Record the failing dependency-boundary check**

Run:

```powershell
Select-String -LiteralPath 'src/AIQ.Exports/AIQ.Exports.csproj' -Pattern 'Google.Apis|ShapeCrawler'
```

Expected: matches for both Google API packages and `ShapeCrawler`. This confirms that the dependency boundary does not meet the design.

- [ ] **Step 2: Move the future source and resources**

Create `src/AIQ.Exports.Future`, then move the two source folders and the two future resource files to the exact paths in the file list. Use tracked moves so Git keeps file history. Do not change namespaces or C# source content.

- [ ] **Step 3: Reduce the stable project definition**

Replace `src/AIQ.Exports/AIQ.Exports.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="AIQ.Exports.Tests" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="ClosedXML" Version="0.105.0" />
    <PackageReference Include="System.Drawing.Common" Version="8.0.0" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Create the future project definition**

Create `src/AIQ.Exports.Future/AIQ.Exports.Future.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <InternalsVisibleTo Include="AIQ.Exports.Future.Tests" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Google.Apis.Slides.v1" Version="1.69.0.3701" />
    <PackageReference Include="Google.Apis.Drive.v3" Version="1.69.0.3790" />
    <PackageReference Include="ShapeCrawler" Version="0.75.2" />
    <PackageReference Include="System.Drawing.Common" Version="8.0.0" />
  </ItemGroup>

</Project>
```

- [ ] **Step 5: Verify the new dependency boundary**

Run:

```powershell
Select-String -LiteralPath 'src/AIQ.Exports/AIQ.Exports.csproj' -Pattern 'Google.Apis|ShapeCrawler'
dotnet build 'src/AIQ.Exports/AIQ.Exports.csproj' -c Release
dotnet build 'src/AIQ.Exports.Future/AIQ.Exports.Future.csproj' -c Release
```

Expected: `Select-String` returns no matches, and both builds succeed.

- [ ] **Step 6: Commit the production split**

Stage only the two production project directories. Confirm that `src/AIQ.Urban.Rhino/AIQ.Urban.RhinoPlugin.csproj` is not staged. Commit with:

```powershell
git commit -m "refactor: split future export features"
```

---

### Task 2: Split the export tests

**Files:**

- Modify: `tests/AIQ.Exports.Tests/AIQ.Exports.Tests.csproj`
- Create: `tests/AIQ.Exports.Future.Tests/AIQ.Exports.Future.Tests.csproj`
- Move: `tests/AIQ.Exports.Tests/GoogleSlides/**` to `tests/AIQ.Exports.Future.Tests/GoogleSlides/**`
- Move: `tests/AIQ.Exports.Tests/ExportTestResources/TestMasterplan.jpg` to `tests/AIQ.Exports.Future.Tests/ExportTestResources/TestMasterplan.jpg`
- Keep: `tests/AIQ.Exports.Tests/Excel/**`

**Interfaces:**

- Consumes: `AIQ.Exports` from the stable tests and `AIQ.Exports.Future` from the future tests
- Produces: xUnit projects `AIQ.Exports.Tests` and `AIQ.Exports.Future.Tests`, each scoped to one production assembly

- [ ] **Step 1: Verify the pre-split test coupling**

Run:

```powershell
Get-ChildItem -LiteralPath 'tests/AIQ.Exports.Tests' -Directory | Select-Object -ExpandProperty Name
```

Expected: the output contains both `Excel` and `GoogleSlides`. This confirms that the test boundary does not meet the design.

- [ ] **Step 2: Move the future tests and test resource**

Create `tests/AIQ.Exports.Future.Tests`, then move the `GoogleSlides` test folder and `TestMasterplan.jpg` to the exact paths in the file list. Do not change test namespaces or test bodies.

- [ ] **Step 3: Remove the unused test package from the stable project**

Remove this line from `tests/AIQ.Exports.Tests/AIQ.Exports.Tests.csproj`:

```xml
<PackageReference Include="Moq" Version="4.20.70" />
```

Keep its existing reference to `src/AIQ.Exports/AIQ.Exports.csproj` and all other xUnit package versions unchanged.

- [ ] **Step 4: Create the future test project**

Create `tests/AIQ.Exports.Future.Tests/AIQ.Exports.Future.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\AIQ.Exports.Future\AIQ.Exports.Future.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

- [ ] **Step 5: Run both export test projects**

Run:

```powershell
dotnet test 'tests/AIQ.Exports.Tests/AIQ.Exports.Tests.csproj' -c Release
dotnet test 'tests/AIQ.Exports.Future.Tests/AIQ.Exports.Future.Tests.csproj' -c Release
```

Expected: both test projects build and all tests pass. Tests that need external Google credentials can retain their current skip or environment behavior; do not weaken an assertion to make a test pass.

- [ ] **Step 6: Commit the test split**

Stage only the two export test directories. Confirm that the Rhino plugin project is not staged. Commit with:

```powershell
git commit -m "test: split future export tests"
```

---

### Task 3: Register projects and verify the plugin dependency graph

**Files:**

- Modify: `ArchSitePlan.sln`
- Inspect only: `src/AIQ.Urban.RhinoInterface/AIQ.Urban.RhinoInterface.csproj`
- Inspect only: `src/AIQ.Urban.Rhino/AIQ.Urban.RhinoPlugin.csproj`
- Update generated graph data: `graphify-out/**`

**Interfaces:**

- Consumes: the four export project files from Tasks 1 and 2
- Produces: solution entries for both new projects and a verified release dependency path from the Rhino plugin to the stable export assembly only

- [ ] **Step 1: Add the new projects to the correct solution folders**

Run:

```powershell
dotnet sln 'ArchSitePlan.sln' add 'src/AIQ.Exports.Future/AIQ.Exports.Future.csproj' --solution-folder src
dotnet sln 'ArchSitePlan.sln' add 'tests/AIQ.Exports.Future.Tests/AIQ.Exports.Future.Tests.csproj' --solution-folder tests
```

Expected: each command reports that it added one project.

- [ ] **Step 2: Verify project references**

Run:

```powershell
Select-String -LiteralPath 'src/AIQ.Urban.RhinoInterface/AIQ.Urban.RhinoInterface.csproj' -Pattern 'AIQ.Exports'
rg -n 'AIQ.Exports.Future' 'src/AIQ.Urban.Rhino' 'src/AIQ.Urban.RhinoInterface'
```

Expected: RhinoInterface references `..\AIQ.Exports\AIQ.Exports.csproj`, and the second command returns no matches.

- [ ] **Step 3: Build the solution and release plugin**

Run:

```powershell
dotnet build 'ArchSitePlan.sln' -c Release
dotnet build 'src/AIQ.Urban.Rhino/AIQ.Urban.RhinoPlugin.csproj' -c Release
```

Expected: both builds succeed when the external UI exists at `..\..\aiqurban-ui\dist\index.html`. If it is absent, record the existing `EnsureVueUiDistExists` error and still complete the project-level and dependency checks. Do not add a fake UI asset to the repository.

- [ ] **Step 4: Inspect the restored plugin dependency assets**

After the release build or restore, run:

```powershell
$assetsPath = 'src/AIQ.Urban.Rhino/obj/project.assets.json'
Select-String -LiteralPath $assetsPath -Pattern 'AIQ.Exports.Future|Google.Apis.Slides|Google.Apis.Drive|ShapeCrawler'
Select-String -LiteralPath $assetsPath -Pattern 'AIQ.Exports/'
```

Expected: the first command returns no matches. The second command confirms that the stable `AIQ.Exports` project is present.

- [ ] **Step 5: Update the knowledge graph**

Run:

```powershell
graphify update .
```

Expected: the update completes and records the new project boundary.

- [ ] **Step 6: Run final workspace checks**

Run:

```powershell
git diff --check
git status --short
```

Expected: there are no whitespace errors. The status shows the intended solution and graph changes plus the user's pre-existing Rhino plugin version change.

- [ ] **Step 7: Commit solution and graph changes**

Stage `ArchSitePlan.sln` and only the graph files changed by `graphify update .`. Confirm again that the Rhino plugin project is not staged. Commit with:

```powershell
git commit -m "build: register future export projects"
```

