# Urban Metrics Excel Export Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a one-click Vue-to-Rhino export that writes the accepted Project Overview and per-Zone urban metrics workbook without leaving partial files.

**Architecture:** The Vue component sends one typed `ExportExcel` request. A Rhino UI-thread message handler owns the native Save dialog and delegates to an injectable exporter; the exporter maps `UrbanModel` metrics into format-neutral report records, `MetricsWorkbookBuilder` constructs the generic Excel document, and `ExcelDocumentWriter` persists it atomically through ClosedXML.

**Tech Stack:** C# 12, .NET 8, RhinoCommon 8, Eto.Forms, ClosedXML 0.105.0, xUnit 2.5.3, NUnit 3.14 with Rhino.Testing, Vue 3.5, TypeScript, Axios, Vite 6.

## Global Constraints

- Use a single **Export Excel** action; there is no separate location/path-selection action.
- The workbook contains `Project Overview` followed by one worksheet per Zone, including empty Zones.
- Coverage and FAR are valid only at Project/Zone scope; Parcel Use has Coverage but no FAR; Built-Plate Use has neither Coverage nor FAR.
- Zone Coverage and FAR use Assigned Parcel Area, never Zone Boundary Area.
- Zone metrics retain representative-point, whole-element assignment; this feature does not clip geometry.
- Zones may overlap; Zone Comparison has no shares or total row.
- Transit rows group by Type + Width and use the colour already shared by the model output and Vue metrics.
- Derived values are guarded Excel formulas; source metrics remain typed fixed numeric values.
- Geometry uses `#,##0.00`, counts `#,##0`, shares/Coverage `0.0%`, and FAR `0.00`.
- Use dark blue `#333C55` with white text for title and table-header bands.
- Do not add charts, review comments, placeholder data, Zone colours, FAR by Use, floor-plate counts, automated Vue tests, or automatic workbook opening.
- Write to a temporary `.xlsx` beside the destination and expose the final path only after the move/replace succeeds.
- Update `graphify-out` with `graphify update .` after code changes.

---

## File Map

### ArchSitePlan repository

- Modify `src/AIQ.Urban.Core/Models/Metrics/ZoneMetrics.cs` — retain Zone boundary and per-Parcel inputs required by the report.
- Modify `src/AIQ.Urban.Core/Models/Metrics/ProjectMetricsCalculator.cs` — supply Zone Boundary Area.
- Modify `src/AIQ.Urban.Rhino/HTML_UI/MessageToUI/ModelStateMessage.cs` — update the synthetic project-wide `ZoneMetrics` construction.
- Modify `src/AIQ.Exports/Excel/Reports/MetricsWorkbookData.cs` — replace worksheet-shaped inputs with report-domain records.
- Rewrite `src/AIQ.Exports/Excel/Reports/MetricsWorkbookBuilder.cs` — implement the accepted workbook contract.
- Modify `src/AIQ.Exports/Excel/Writing/ExcelDocumentWriter.cs` — persist through a same-directory temporary file.
- Create `src/AIQ.Urban.RhinoInterface/Exports/IUrbanMetricsExcelExporter.cs` — injectable exporter boundary for Rhino orchestration.
- Modify `src/AIQ.Urban.RhinoInterface/Exports/UrbanMetricsExcelExporter.cs` — map the model and write the report.
- Create `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/IExcelSaveDialog.cs` — save-dialog seam.
- Create `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/RhinoExcelSaveDialog.cs` — Eto/Rhino implementation.
- Create `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/ExportExcelMessage.cs` — export orchestration.
- Create `src/AIQ.Urban.Rhino/HTML_UI/MessageToUI/ExcelExportResultMessage.cs` — typed success/cancel/failure response.
- Modify `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/IMessageFromUI.cs` — add `ExportExcel` to `TypeOfMessage`.
- Modify `src/AIQ.Urban.Rhino/HTML_UI/MessageConverter.cs` — route `ExportExcel`.
- Modify `tests/AIQ.Urban.Core.Tests/Models/Metrics/ProjectMetricsCalculatorTests.cs` — Zone reporting-input coverage.
- Modify `tests/AIQ.Exports.Tests/Excel/ExcelDocumentWriterTests.cs` — atomic write coverage.
- Rewrite `tests/AIQ.Exports.Tests/Excel/MetricsWorkbookBuilderTests.cs` — accepted workbook and integration coverage.
- Modify `tests/AIQ.Urban.RhinoPlugin.Tests/Exports/UrbanMetricsExcelExporterTests.cs` — mapping coverage.
- Modify `tests/AIQ.Urban.RhinoPlugin.Tests/HTML_UI/MessageConverterTests.cs` — message routing coverage.
- Create `tests/AIQ.Urban.RhinoPlugin.Tests/HTML_UI/ExportExcelMessageTests.cs` — success/cancel/failure orchestration coverage.

### aiqurban-ui repository

- Create `src/components/MESSAGES/ExcelExportResultMessage.ts` — frontend result type.
- Modify `src/services/RhinoApiService.ts` — decode the new result.
- Rewrite `src/components/EXPORTS/PluginExports.vue` — one-click export state and feedback.

---

### Task 1: Retain Zone reporting inputs in core metrics

**Files:**
- Modify: `src/AIQ.Urban.Core/Models/Metrics/ZoneMetrics.cs`
- Modify: `src/AIQ.Urban.Core/Models/Metrics/ProjectMetricsCalculator.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessageToUI/ModelStateMessage.cs`
- Test: `tests/AIQ.Urban.Core.Tests/Models/Metrics/ProjectMetricsCalculatorTests.cs`

**Interfaces:**
- Consumes: `Zone.Boundary.Area()` and the `List<ParcelMetrics>` already passed into `ZoneMetrics`.
- Produces: `ZoneMetrics.ZoneBoundaryArea : double` and `ZoneMetrics.ParcelMetrics : List<ParcelMetrics>` for Task 4.

- [ ] **Step 1: Write the failing NUnit test**

Add a focused test using the existing primitive geometry helpers:

```csharp
[Test]
public void CalculateAndStore_Zone_RetainsBoundaryAreaAndAssignedParcelMetrics()
{
    // Arrange
    var model = UrbanModel.Instance;
    var use = model.GetUse("Residential");
    var parcel = CreateParcel(0, 0, 10, use);
    var zone = new Zone(CreatePolygon(-1, -1, 12), "Test Zone");
    model.Parcels.Add(parcel);
    model.Zones.Add(zone);

    // Act
    ProjectMetricsCalculator.CalculateAndStore(model, containmentTolerance: 0.001);

    // Preview
    var zoneMetrics = model.ProjectMetrics!.ZoneMetrics.Single();
    TestContext.WriteLine(
        $"Boundary: {zoneMetrics.ZoneBoundaryArea}; parcels: {zoneMetrics.ParcelMetrics.Count}");

    // Assert
    Assert.That(zoneMetrics.ZoneBoundaryArea, Is.EqualTo(144).Within(1e-6));
    Assert.That(zoneMetrics.ParcelMetrics, Has.Count.EqualTo(1));
    Assert.That(zoneMetrics.ParcelMetrics.Single().SourceElementId, Is.EqualTo(parcel.Id));
}
```

Update `Constructor_SyntheticAllZone_UsesEmptySourceElementId` to pass `zoneBoundaryArea: 0`.

- [ ] **Step 2: Run the test to verify it fails**

Run:

```powershell
dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj --filter "FullyQualifiedName~ProjectMetricsCalculatorTests.CalculateAndStore_Zone_RetainsBoundaryAreaAndAssignedParcelMetrics"
```

Expected: compilation fails because `ZoneMetrics` has no `ZoneBoundaryArea` or `ParcelMetrics` property.

- [ ] **Step 3: Add the minimal core properties and constructor parameter**

In `ZoneMetrics.cs`, add and assign the exact members:

```csharp
public double ZoneBoundaryArea { get; }
public List<ParcelMetrics> ParcelMetrics { get; }

public ZoneMetrics(
    Guid sourceElementId,
    string zoneName,
    double zoneBoundaryArea,
    List<BuildingMetrics> buildings,
    List<ParcelMetrics> parcels,
    IEnumerable<Transit> transits)
{
    SourceElementId = sourceElementId;
    ZoneName = zoneName;
    ZoneBoundaryArea = zoneBoundaryArea;
    ParcelMetrics = parcels;
}
```

Keep the existing assignments and aggregate calculations after those four assignments: counts, total areas, `TransitMetrics`, Use roll-ups, Coverage, and FAR do not change their formulas.

In `ProjectMetricsCalculator.ComputeZoneMetrics`, supply the geometric boundary area:

```csharp
return new ZoneMetrics(
    zone.Id,
    zone.Name,
    zone.Boundary.Area(),
    zoneBuildingMetrics,
    zoneParcelMetrics,
    containedTransits);
```

In `ModelStateMessage.BuildStudyData`, keep the synthetic `All` scope valid without introducing a fake Zone boundary:

```csharp
var projectWideZoneMetrics = new ZoneMetrics(
    Guid.Empty,
    "All",
    zoneBoundaryArea: 0,
    buildings: projectMetrics.BuildingMetrics,
    parcels: projectMetrics.ParcelMetrics,
    transits: model.Transits);
```

- [ ] **Step 4: Run the focused and full core metric tests**

Run:

```powershell
dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj --filter "FullyQualifiedName~ProjectMetricsCalculatorTests"
```

Expected: all `ProjectMetricsCalculatorTests` pass.

- [ ] **Step 5: Commit the core metric change**

```powershell
git add src/AIQ.Urban.Core/Models/Metrics/ZoneMetrics.cs src/AIQ.Urban.Core/Models/Metrics/ProjectMetricsCalculator.cs src/AIQ.Urban.Rhino/HTML_UI/MessageToUI/ModelStateMessage.cs tests/AIQ.Urban.Core.Tests/Models/Metrics/ProjectMetricsCalculatorTests.cs
git commit -m "feat: retain zone metrics export inputs"
```

---

### Task 2: Make Excel persistence atomic

**Files:**
- Modify: `src/AIQ.Exports/Excel/Writing/ExcelDocumentWriter.cs`
- Test: `tests/AIQ.Exports.Tests/Excel/ExcelDocumentWriterTests.cs`

**Interfaces:**
- Consumes: existing `ExcelDocumentWriter.Write(ExcelDocument document, string filePath)` callers.
- Produces: the same public signature with no partial destination and no remaining temporary file after failure.

- [ ] **Step 1: Write failing xUnit tests for replace and cleanup**

Add two tests, using an injected post-processing failure to force an exception after ClosedXML has created the temporary workbook:

```csharp
[Fact]
public void Write_ExistingDestination_ReplacesWorkbook()
{
    var outputPath = TemporaryWorkbookPath();
    try
    {
        var writer = new ExcelDocumentWriter();
        writer.Write(CreateTextDocument("Before"), outputPath);
        writer.Write(CreateTextDocument("After"), outputPath);

        using var workbook = new XLWorkbook(outputPath);
        Assert.Equal("After", workbook.Worksheet("Summary").Cell("A1").GetString());
    }
    finally
    {
        DeleteIfPresent(outputPath);
    }
}

[Fact]
public void Write_WriterFails_RemovesTemporaryWorkbookAndPreservesDestination()
{
    var outputPath = TemporaryWorkbookPath();
    File.WriteAllText(outputPath, "original");
    var document = CreateTextDocument("replacement");
    var writer = new ExcelDocumentWriter(
        _ => throw new InvalidOperationException("post-processing failed"));

    try
    {
        Assert.Throws<InvalidOperationException>(
            () => writer.Write(document, outputPath));

        Assert.Equal("original", File.ReadAllText(outputPath));
        var pattern = $".{Path.GetFileName(outputPath)}.*.tmp.xlsx";
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(outputPath)!, pattern));
    }
    finally
    {
        DeleteIfPresent(outputPath);
    }
}
```

Add this explicit helper, avoiding test-only reuse of production internals:

```csharp
private static ExcelDocument CreateTextDocument(string value)
{
    var block = new TableBlockBuilder()
        .AddNewRow([ExcelCell.From(value)])
        .Build();
    return CreateDocument("Summary", [block], new ExcelCellAddress(1, 1));
}
```

- [ ] **Step 2: Run the tests to verify failure**

Run:

```powershell
dotnet test tests/AIQ.Exports.Tests/AIQ.Exports.Tests.csproj --filter "FullyQualifiedName~ExcelDocumentWriterTests.Write_ExistingDestination_ReplacesWorkbook|FullyQualifiedName~ExcelDocumentWriterTests.Write_WriterFails_RemovesTemporaryWorkbookAndPreservesDestination"
```

Expected: compilation fails because `ExcelDocumentWriter` has no injectable post-processor constructor.

- [ ] **Step 3: Refactor `Write` around a temporary path**

Add a narrow internal constructor seam around the existing fill-format post-processing:

```csharp
private readonly Action<string> _postProcess;

public ExcelDocumentWriter()
    : this(EnsureFillFormatsApplied)
{
}

internal ExcelDocumentWriter(Action<string> postProcess)
{
    _postProcess = postProcess;
}
```

Keep validation before creating the temporary file. Move the current workbook construction body into `WriteWorkbook` and make `Write` coordinate persistence:

```csharp
public void Write(ExcelDocument document, string filePath)
{
    ArgumentNullException.ThrowIfNull(document);
    ValidateDestination(document, filePath);

    var absolutePath = Path.GetFullPath(filePath);
    var directory = Path.GetDirectoryName(absolutePath)!;
    var temporaryPath = Path.Combine(
        directory,
        $".{Path.GetFileName(absolutePath)}.{Guid.NewGuid():N}.tmp.xlsx");

    try
    {
        WriteWorkbook(document, temporaryPath);

        if (File.Exists(absolutePath))
            File.Replace(temporaryPath, absolutePath, destinationBackupFileName: null);
        else
            File.Move(temporaryPath, absolutePath);
    }
    finally
    {
        if (File.Exists(temporaryPath))
            File.Delete(temporaryPath);
    }
}
```

`ValidateDestination` contains the existing null/path/directory/worksheet-name checks. `WriteWorkbook` contains the existing `XLWorkbook` loop, `RecalculateAllFormulas`, and `SaveAs`, then calls `_postProcess(filePath)`. The public constructor still uses `EnsureFillFormatsApplied`; only tests inject a failure.

- [ ] **Step 4: Run all writer tests**

Run:

```powershell
dotnet test tests/AIQ.Exports.Tests/AIQ.Exports.Tests.csproj --filter "FullyQualifiedName~ExcelDocumentWriterTests"
```

Expected: all `ExcelDocumentWriterTests` pass and no `*.tmp.xlsx` files remain.

- [ ] **Step 5: Commit atomic persistence**

```powershell
git add src/AIQ.Exports/Excel/Writing/ExcelDocumentWriter.cs tests/AIQ.Exports.Tests/Excel/ExcelDocumentWriterTests.cs
git commit -m "feat: write Excel documents atomically"
```

---

### Task 3: Build the accepted workbook from report records

**Files:**
- Modify: `src/AIQ.Exports/Excel/Reports/MetricsWorkbookData.cs`
- Rewrite: `src/AIQ.Exports/Excel/Reports/MetricsWorkbookBuilder.cs`
- Rewrite: `tests/AIQ.Exports.Tests/Excel/MetricsWorkbookBuilderTests.cs`

**Interfaces:**
- Consumes: format-neutral records defined in this task.
- Produces: `MetricsWorkbookBuilder.Build(MetricsWorkbookData data) : ExcelDocument` for Task 4.

- [ ] **Step 1: Define the report-record contract in the failing tests**

Tests construct these exact records:

```csharp
public sealed record MetricsWorkbookData(
    string ProjectName,
    DateTime ExportedAt,
    MetricsScopeData Project,
    IReadOnlyList<MetricsScopeData> Zones);

public sealed record MetricsScopeData(
    string DisplayName,
    double? ZoneBoundaryArea,
    double ParcelArea,
    double BuildingFootprint,
    double BuiltArea,
    double TransitLength,
    double TransitArea,
    int BuildingCount,
    int ParcelCount,
    int TransitCount,
    IReadOnlyList<ParcelUseData> ParcelUses,
    IReadOnlyList<BuiltPlateUseData> BuiltPlateUses,
    IReadOnlyList<TransitMetricData> TransitMetrics);

public sealed record ParcelUseData(
    string UseName,
    Color Color,
    int ParcelCount,
    double Area,
    double SumOfParcelPerimeters,
    double BuildingFootprint);

public sealed record BuiltPlateUseData(
    string UseName,
    Color Color,
    double BuiltArea);

public sealed record TransitMetricData(
    string Type,
    double Width,
    double RunningLength,
    double Area,
    Color Color);
```

Rewrite `MetricsWorkbookBuilderTests` around one representative Project, two Zones whose sanitized names collide, and one empty Zone. Add focused facts with these assertions:

```csharp
[Fact]
public void Build_RepresentativeReport_CreatesAcceptedProjectAndZoneTables()
{
    var document = MetricsWorkbookBuilder.Build(CreateWorkbookData());
    var outputPath = TemporaryWorkbookPath();

    try
    {
        new ExcelDocumentWriter().Write(document, outputPath);
        using var workbook = new XLWorkbook(outputPath);

        Assert.Equal(
            ["Project Overview", "Zone - North-Zone", "Zone - North-Zone (2)", "Zone - Empty"],
            workbook.Worksheets.Select(sheet => sheet.Name));

        var project = workbook.Worksheet("Project Overview");
        Assert.Equal("Sample Project", project.Cell("A1").GetString());
        Assert.Equal("Exported: 2026-08-04 14:30", project.Cell("A2").GetString());
        Assert.NotNull(FindCell(project, "PROJECT SUMMARY"));
        Assert.NotNull(FindCell(project, "PARCEL USE SUMMARY"));
        Assert.NotNull(FindCell(project, "BUILT-PLATE USE SUMMARY"));
        Assert.NotNull(FindCell(project, "TRANSIT SUMMARY"));
        Assert.NotNull(FindCell(project, "ZONE COMPARISON"));
        Assert.DoesNotContain(project.CellsUsed(), cell => cell.GetString() == "FAR by Use");
        Assert.DoesNotContain(project.CellsUsed(), cell => cell.GetString() == "Floor Plate Count");

        var zone = workbook.Worksheet("Zone - North-Zone");
        Assert.NotNull(FindCell(zone, "ZONE SUMMARY"));
        Assert.Null(FindOptionalCell(zone, "ZONE COMPARISON"));
        Assert.Equal("North/Zone", zone.Cell("A1").GetString());
    }
    finally
    {
        DeleteIfPresent(outputPath);
    }
}
```

```csharp
[Fact]
public void Build_DerivedCells_UsesGuardedFormulasAndAcceptedFormats()
{
    var outputPath = WriteWorkbook(CreateWorkbookData());
    try
    {
        using var workbook = new XLWorkbook(outputPath);
        var project = workbook.Worksheet("Project Overview");
        var coverage = FindCell(project, "Coverage").CellRight();
        var far = FindCell(project, "FAR").CellRight();
        Assert.Contains("IF(", coverage.FormulaA1);
        Assert.Contains("=0,0", coverage.FormulaA1);
        Assert.Equal("0.0%", coverage.Style.NumberFormat.Format);
        Assert.Contains("IF(", far.FormulaA1);
        Assert.Equal("0.00", far.Style.NumberFormat.Format);

        var residential = FindCell(project, "Residential");
        Assert.Equal("#,##0.00", residential.CellRight(3).Style.NumberFormat.Format);
        Assert.Equal("0.0%", residential.CellRight(4).Style.NumberFormat.Format);
    }
    finally
    {
        DeleteIfPresent(outputPath);
    }
}
```

```csharp
[Fact]
public void Build_EmptyZone_PreservesEachExpectedTableWithNoDataRow()
{
    var outputPath = WriteWorkbook(CreateWorkbookData());
    try
    {
        using var workbook = new XLWorkbook(outputPath);
        var zone = workbook.Worksheet("Zone - Empty");
        Assert.Equal(3, zone.CellsUsed().Count(cell => cell.GetString() == "No data"));
        Assert.NotNull(FindCell(zone, "PARCEL USE SUMMARY"));
        Assert.NotNull(FindCell(zone, "BUILT-PLATE USE SUMMARY"));
        Assert.NotNull(FindCell(zone, "TRANSIT SUMMARY"));
    }
    finally
    {
        DeleteIfPresent(outputPath);
    }
}
```

Add these focused tests for Transit aggregation and non-additive Zone Comparison:

```csharp
[Fact]
public void Build_TransitRows_GroupsTypeAndWidthWithSubtotalsAndGrandTotal()
{
    var outputPath = WriteWorkbook(CreateWorkbookData());
    try
    {
        using var workbook = new XLWorkbook(outputPath);
        var project = workbook.Worksheet("Project Overview");
        var roadWidthSix = project.RowsUsed()
            .Single(row =>
                row.Cell(1).GetString() == "Road"
                && row.Cell(2).GetDouble() == 6);

        Assert.Equal(2, roadWidthSix.Cell(3).GetDouble());
        Assert.Equal(30, roadWidthSix.Cell(4).GetDouble());
        Assert.Equal(
            XLColor.FromColor(Color.FromArgb(90, 100, 110)),
            roadWidthSix.Cell(8).Style.Fill.BackgroundColor);
        Assert.NotNull(FindCell(project, "Road Total"));
        Assert.NotNull(FindCell(project, "Grand Total"));
    }
    finally
    {
        DeleteIfPresent(outputPath);
    }
}

[Fact]
public void Build_ZoneComparison_HasAcceptedColumnsAndNoTotalRow()
{
    var outputPath = WriteWorkbook(CreateWorkbookData());
    try
    {
        using var workbook = new XLWorkbook(outputPath);
        var project = workbook.Worksheet("Project Overview");
        var heading = FindCell(project, "ZONE COMPARISON");
        var headerRow = heading.WorksheetRow().RowBelow();
        Assert.Equal(
            [
                "Zone", "Zone Boundary Area", "Assigned Parcel Area",
                "Building Footprint", "Coverage", "Built Area", "FAR",
                "Transit Length", "Transit Area",
            ],
            headerRow.Cells(1, 9).Select(cell => cell.GetString()));

        var nextSectionOrEnd = project.LastRowUsed()!.RowNumber();
        Assert.DoesNotContain(
            project.Rows(heading.Address.RowNumber + 2, nextSectionOrEnd),
            row => row.Cell(1).GetString() is "Total" or "Grand Total");
    }
    finally
    {
        DeleteIfPresent(outputPath);
    }
}
```

Define the representative input explicitly so every test uses the same fixed metric values:

```csharp
private static MetricsWorkbookData CreateWorkbookData()
{
    var project = new MetricsScopeData(
        DisplayName: "Sample Project",
        ZoneBoundaryArea: null,
        ParcelArea: 1_000,
        BuildingFootprint: 200,
        BuiltArea: 600,
        TransitLength: 35,
        TransitArea: 230,
        BuildingCount: 3,
        ParcelCount: 2,
        TransitCount: 3,
        ParcelUses:
        [
            new ParcelUseData(
                "Residential", Color.Gold, 2, 1_000, 140, 200),
        ],
        BuiltPlateUses:
        [
            new BuiltPlateUseData("Residential", Color.Gold, 600),
        ],
        TransitMetrics:
        [
            new TransitMetricData("Road", 6, 10, 60, Color.FromArgb(90, 100, 110)),
            new TransitMetricData("Road", 6, 20, 120, Color.FromArgb(90, 100, 110)),
            new TransitMetricData("Road", 10, 5, 50, Color.FromArgb(70, 80, 90)),
        ]);

    var firstZone = project with
    {
        DisplayName = "North/Zone",
        ZoneBoundaryArea = 400,
        ParcelArea = 300,
        BuildingFootprint = 60,
        BuiltArea = 180,
    };
    var collidingZone = project with
    {
        DisplayName = "North:Zone",
        ZoneBoundaryArea = 500,
        ParcelArea = 350,
        BuildingFootprint = 70,
        BuiltArea = 210,
    };
    var emptyZone = new MetricsScopeData(
        "Empty", 100, 0, 0, 0, 0, 0, 0, 0, 0, [], [], []);

    return new MetricsWorkbookData(
        "Sample Project",
        new DateTime(2026, 8, 4, 14, 30, 0),
        project,
        [firstZone, collidingZone, emptyZone]);
}

private static string WriteWorkbook(MetricsWorkbookData data)
{
    var path = TemporaryWorkbookPath();
    new ExcelDocumentWriter().Write(MetricsWorkbookBuilder.Build(data), path);
    return path;
}

private static IXLCell FindCell(IXLWorksheet worksheet, string value) =>
    worksheet.CellsUsed().Single(cell => cell.GetString() == value);

private static IXLCell? FindOptionalCell(IXLWorksheet worksheet, string value) =>
    worksheet.CellsUsed().FirstOrDefault(cell => cell.GetString() == value);

private static string TemporaryWorkbookPath() =>
    Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-metrics.xlsx");

private static void DeleteIfPresent(string path)
{
    if (File.Exists(path))
        File.Delete(path);
}
```

- [ ] **Step 2: Run the builder tests to verify compilation failure**

Run:

```powershell
dotnet test tests/AIQ.Exports.Tests/AIQ.Exports.Tests.csproj --filter "FullyQualifiedName~MetricsWorkbookBuilderTests"
```

Expected: compilation fails because the current records and builder expose the old worksheet-shaped contract.

- [ ] **Step 3: Replace `MetricsWorkbookData.cs` with the exact records above**

Keep `using System.Drawing;` and put all five immutable records in `AIQ.Exports.Excel.Reports`. Do not add Rhino or Urban Core references to `AIQ.Exports`.

- [ ] **Step 4: Implement workbook creation and worksheet naming**

Use `ReportColumnCount = 9`, make title and header fill `Color.FromArgb(51, 60, 85)`, and create sheets in this order:

```csharp
public static ExcelDocument Build(MetricsWorkbookData data)
{
    ArgumentNullException.ThrowIfNull(data);

    var worksheets = new List<ExcelWorksheetDefinition>
    {
        BuildProjectWorksheet(data),
    };

    var zoneNames = ResolveZoneWorksheetNames(
        data.Zones.Select(zone => zone.DisplayName));
    worksheets.AddRange(data.Zones.Zip(zoneNames, (zone, name) =>
        BuildZoneWorksheet(data.ExportedAt, zone, name)));

    return new ExcelDocument(worksheets);
}
```

`ResolveZoneWorksheetNames` starts from `Zone - {displayName}`, replaces `[ ] : * ? / \` with `-`, trims apostrophes, truncates to 31 characters, and truncates the base again before appending ` (2)`, ` (3)`. Reserve `Project Overview` in the case-insensitive used-name set.

- [ ] **Step 5: Implement title and summary sections with cell-reference formulas**

Both worksheet builders add the full display name at `A1` and this timestamp at `A2`:

```csharp
$"Exported: {exportedAt:yyyy-MM-dd HH:mm}"
```

Project uses `PROJECT SUMMARY`; Zone uses `ZONE SUMMARY` and includes `Zone Boundary Area` before `Assigned Parcel Area`. Capture the value-cell addresses returned by `AddMetric` and add formulas instead of fixed ratios:

```csharp
composer.AddFormulaMetric(
    "Coverage",
    $"IF({parcelAreaCell}=0,0,{footprintCell}/{parcelAreaCell})",
    "ratio",
    PercentageStyle);
composer.AddFormulaMetric(
    "FAR",
    $"IF({parcelAreaCell}=0,0,{builtAreaCell}/{parcelAreaCell})",
    "ratio",
    RatioStyle);
```

Use metric labels `Parcel Area`, `Building Footprint`, `Built Area`, `Transit Length`, `Transit Area`, `Building Count`, `Parcel Count`, and `Transit Count`. Use `units²`, `units`, `count`, and `ratio` in the Unit column.

- [ ] **Step 6: Implement the three detail tables**

Use these exact headers:

```csharp
string[] parcelHeaders =
[
    "Use", "Colour", "Parcel Count", "Area", "Area Share",
    "Sum of Parcel Perimeters", "Building Footprint on Parcels", "Coverage",
];

string[] builtPlateHeaders =
[
    "Floor-Plate Use", "Colour", "Built Area", "Built Area Share",
];

string[] transitHeaders =
[
    "Type", "Width", "Corridor Count", "Running Length", "Length Share",
    "Transit Area", "Area Share", "Colour",
];
```

Sort Use rows case-insensitively. Add `Total` rows for non-empty Parcel and Built-Plate tables. Make shares divide each row by its table total with `IF(total=0,0,row/total)`. Make Parcel Use Coverage divide Building Footprint on Parcels by that row's Area with the same guard. Do not add FAR to either Use table.

Group Transit source rows by Type and Width. Sum count, Running Length, and Area; retain the first group colour after asserting all same-group colours are equal. Add one subtotal after each Type and one Grand Total. Every Length Share and Area Share divides by the Grand Total cells, including Type subtotals. Empty detail tables add exactly one merged-style row whose first cell is `No data` and do not add a total row.

Enforce the colour invariant before adding a detail row:

```csharp
var colors = widthGroup.Select(row => row.Color).Distinct().ToList();
if (colors.Count != 1)
{
    throw new InvalidOperationException(
        $"Transit rows for {widthGroup.Key.Type} at width "
        + $"{widthGroup.Key.Width} have inconsistent colours.");
}
```

- [ ] **Step 7: Implement Project-only Zone Comparison**

Use exactly these headers:

```csharp
string[] zoneHeaders =
[
    "Zone", "Zone Boundary Area", "Assigned Parcel Area", "Building Footprint",
    "Coverage", "Built Area", "FAR", "Transit Length", "Transit Area",
];
```

Add one row per Zone, no colour, perimeter, share, subtotal, or total. Coverage and FAR are guarded formulas using that row's Assigned Parcel Area. An empty Zone list produces one `No data` row.

- [ ] **Step 8: Run the builder/integration tests**

Run:

```powershell
dotnet test tests/AIQ.Exports.Tests/AIQ.Exports.Tests.csproj --filter "FullyQualifiedName~MetricsWorkbookBuilderTests"
```

Expected: all builder tests pass after writing and reopening real `.xlsx` files; formulas recalculate without error values.

- [ ] **Step 9: Run all Excel export tests and commit**

Run:

```powershell
dotnet test tests/AIQ.Exports.Tests/AIQ.Exports.Tests.csproj --filter "FullyQualifiedName~AIQ.Exports.Tests.Excel"
```

Expected: all Excel tests pass.

```powershell
git add src/AIQ.Exports/Excel/Reports/MetricsWorkbookData.cs src/AIQ.Exports/Excel/Reports/MetricsWorkbookBuilder.cs tests/AIQ.Exports.Tests/Excel/MetricsWorkbookBuilderTests.cs
git commit -m "feat: build urban metrics Excel report"
```

---

### Task 4: Map `UrbanModel` metrics into the report contract

**Files:**
- Create: `src/AIQ.Urban.RhinoInterface/Exports/IUrbanMetricsExcelExporter.cs`
- Modify: `src/AIQ.Urban.RhinoInterface/Exports/UrbanMetricsExcelExporter.cs`
- Test: `tests/AIQ.Urban.RhinoPlugin.Tests/Exports/UrbanMetricsExcelExporterTests.cs`

**Interfaces:**
- Consumes: `MetricsWorkbookData`, `ZoneMetrics.ParcelMetrics`, `ZoneMetrics.ZoneBoundaryArea`, Transit Type + Width, and `Transit.Color`.
- Produces: `IUrbanMetricsExcelExporter.Export(UrbanModel urbanModel, string filePath, string projectName, DateTime exportedAt)` and internal `BuildWorkbookData(UrbanModel urbanModel, string projectName, DateTime exportedAt)`.

- [ ] **Step 1: Rewrite the NUnit mapping tests against the new interface**

Retain the missing-metrics test but instantiate the exporter:

```csharp
var exception = Assert.Throws<InvalidOperationException>(() =>
    new UrbanMetricsExcelExporter().Export(
        UrbanModel.Instance,
        outputPath,
        "Unsaved Project",
        new DateTime(2026, 8, 4, 14, 30, 0)));
```

Replace the old mapping test with assertions for all new inputs:

```csharp
var exportedAt = new DateTime(2026, 8, 4, 14, 30, 0);
var transitColor = Color.FromArgb(80, 90, 100);
firstTransit.Color = transitColor;
secondTransit.Color = transitColor;

var workbookData = UrbanMetricsExcelExporter.BuildWorkbookData(
    model,
    "Sample Project",
    exportedAt);

TestContext.WriteLine(
    $"Project: {workbookData.ProjectName}; Zones: {workbookData.Zones.Count}");

Assert.That(workbookData.ProjectName, Is.EqualTo("Sample Project"));
Assert.That(workbookData.ExportedAt, Is.EqualTo(exportedAt));
Assert.That(workbookData.Project.ParcelUses.Single().ParcelCount, Is.EqualTo(1));
Assert.That(workbookData.Project.ParcelUses.Single().Area, Is.EqualTo(100).Within(1e-6));
Assert.That(workbookData.Project.ParcelUses.Single().SumOfParcelPerimeters, Is.EqualTo(40).Within(1e-6));
Assert.That(workbookData.Project.ParcelUses.Single().BuildingFootprint, Is.EqualTo(4).Within(1e-6));
Assert.That(workbookData.Project.BuiltPlateUses.Single().BuiltArea, Is.EqualTo(8).Within(1e-6));
Assert.That(workbookData.Project.TransitMetrics.All(row => row.Color == transitColor), Is.True);
Assert.That(workbookData.Zones.Single().ZoneBoundaryArea, Is.EqualTo(900).Within(1e-6));
Assert.That(workbookData.Zones.Single().ParcelArea, Is.EqualTo(100).Within(1e-6));
```

- [ ] **Step 2: Run the exporter tests to verify compilation failure**

Run:

```powershell
dotnet test tests/AIQ.Urban.RhinoPlugin.Tests/AIQ.Urban.RhinoInterface.Tests.csproj --filter "FullyQualifiedName~UrbanMetricsExcelExporterTests"
```

Expected: compilation fails because the exporter is static and returns the old report model.

- [ ] **Step 3: Add the injectable exporter interface**

Create:

```csharp
using AIQ.Urban.Core;

namespace AIQ.Urban.RhinoInterface.Exports;

public interface IUrbanMetricsExcelExporter
{
    void Export(
        UrbanModel urbanModel,
        string filePath,
        string projectName,
        DateTime exportedAt);
}
```

Convert `UrbanMetricsExcelExporter` from a static class to a sealed implementation. Its public method validates metrics, builds the report, and calls the atomic writer:

```csharp
public sealed class UrbanMetricsExcelExporter : IUrbanMetricsExcelExporter
{
    public void Export(
        UrbanModel urbanModel,
        string filePath,
        string projectName,
        DateTime exportedAt)
    {
        var report = BuildWorkbookData(
            urbanModel,
            projectName,
            exportedAt);
        var document = MetricsWorkbookBuilder.Build(report);
        new ExcelDocumentWriter().Write(document, filePath);
    }
}
```

- [ ] **Step 4: Implement Project and Zone scope mapping**

Guard `urbanModel.ProjectMetrics` with the existing clear exception. Build Project from `ProjectMetrics.ParcelMetrics`, `BuildingMetrics`, and `TransitMetrics`; build each Zone from its retained `ParcelMetrics`, roll-ups, and `ZoneBoundaryArea`.

Map Parcel Use from per-Parcel data, not `LandAreaByUse`:

```csharp
private static List<ParcelUseData> MapParcelUses(
    IEnumerable<ParcelMetrics> parcels)
{
    return parcels
        .GroupBy(parcel => parcel.Use)
        .Select(group => new ParcelUseData(
            group.Key.Name,
            group.Key.Color,
            group.Count(),
            group.Sum(parcel => parcel.LandArea),
            group.Sum(parcel => parcel.Perimeter),
            group.Sum(parcel => parcel.BuiltFootprint)))
        .OrderBy(row => row.UseName, StringComparer.OrdinalIgnoreCase)
        .ToList();
}
```

Map Built-Plate Use only from the existing `(Use, Area)` roll-up. Do not calculate Use FAR.

- [ ] **Step 5: Map Transit colour by Type + Width**

Build a colour lookup from the current model and use the same fallback currently used by `ModelStateMessage`. Type + Width is the shared presentation key used by the Rhino output, Vue charts, and workbook:

```csharp
var transitColors = urbanModel.Transits
    .GroupBy(transit => (transit.TransitType, transit.Width))
    .ToDictionary(
        group => group.Key,
        group => group.First().Color ?? Color.DarkGray);

private static List<TransitMetricData> MapTransitMetrics(
    IEnumerable<TransitMetrics> metrics,
    IReadOnlyDictionary<(TransitType Type, double Width), Color> transitColors)
{
    return metrics
        .Select(metric => new TransitMetricData(
            metric.Type.ToString(),
            metric.Width,
            metric.RunningLength,
            metric.Area,
            transitColors.GetValueOrDefault(
                (metric.Type, metric.Width),
                Color.DarkGray)))
        .ToList();
}
```

This deliberately reads the model colour already consumed by Rhino/Vue and does not create a second colour algorithm.

- [ ] **Step 6: Run exporter tests and commit**

Run:

```powershell
dotnet test tests/AIQ.Urban.RhinoPlugin.Tests/AIQ.Urban.RhinoInterface.Tests.csproj --filter "FullyQualifiedName~UrbanMetricsExcelExporterTests"
```

Expected: all exporter mapping tests pass.

```powershell
git add src/AIQ.Urban.RhinoInterface/Exports/IUrbanMetricsExcelExporter.cs src/AIQ.Urban.RhinoInterface/Exports/UrbanMetricsExcelExporter.cs tests/AIQ.Urban.RhinoPlugin.Tests/Exports/UrbanMetricsExcelExporterTests.cs
git commit -m "feat: map urban metrics into Excel report"
```

---

### Task 5: Add Rhino save-dialog orchestration and typed results

**Files:**
- Create: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/IExcelSaveDialog.cs`
- Create: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/RhinoExcelSaveDialog.cs`
- Create: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/ExportExcelMessage.cs`
- Create: `src/AIQ.Urban.Rhino/HTML_UI/MessageToUI/ExcelExportResultMessage.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/IMessageFromUI.cs`
- Modify: `src/AIQ.Urban.Rhino/HTML_UI/MessageConverter.cs`
- Modify: `tests/AIQ.Urban.RhinoPlugin.Tests/HTML_UI/MessageConverterTests.cs`
- Create: `tests/AIQ.Urban.RhinoPlugin.Tests/HTML_UI/ExportExcelMessageTests.cs`

**Interfaces:**
- Consumes: `IUrbanMetricsExcelExporter` from Task 4 and the existing `HttpUiServer` UI-thread dispatch.
- Produces: request `{"messageType":"ExportExcel"}` and response `{ type, status, filePath, errorMessage }` for Task 6.

- [ ] **Step 1: Write the converter and orchestration NUnit tests**

Add to `MessageConverterTests`:

```csharp
[Test]
public void Convert_ExportExcelMessage_ReturnsExportExcelMessage()
{
    var message = MessageConverter.Convert("{\"messageType\":\"ExportExcel\"}");
    Assert.That(message, Is.TypeOf<ExportExcelMessage>());
}
```

In the new test file, use fakes implementing the interfaces and cover four focused behaviors:

```csharp
[Test]
public void HandleMessage_UserCancels_ReturnsCancelledWithoutExporting()
{
    var dialog = new FakeSaveDialog(result: null);
    var exporter = new FakeExporter();
    var message = CreateMessage(dialog, exporter, projectName: "Sample.3dm");

    var result = message.HandleMessage();

    TestContext.WriteLine($"Status: {result.Status}");
    Assert.That(result.Status, Is.EqualTo("Cancelled"));
    Assert.That(exporter.CallCount, Is.Zero);
}
```

```csharp
[Test]
public void HandleMessage_Confirmed_ExportsAndReturnsFinalPath()
{
    const string path = @"C:\Exports\Sample Project - Urban Metrics.xlsx";
    SetCalculatedMetrics();
    var dialog = new FakeSaveDialog(path);
    var exporter = new FakeExporter();
    var exportedAt = new DateTime(2026, 8, 4, 14, 30, 0);
    var message = CreateMessage(
        dialog,
        exporter,
        projectName: "Sample Project.3dm",
        exportedAt);

    var result = message.HandleMessage();

    Assert.That(dialog.SuggestedFileName, Is.EqualTo("Sample Project - Urban Metrics.xlsx"));
    Assert.That(exporter.CallCount, Is.EqualTo(1));
    Assert.That(exporter.ProjectName, Is.EqualTo("Sample Project"));
    Assert.That(exporter.ExportedAt, Is.EqualTo(exportedAt));
    Assert.That(result.Status, Is.EqualTo("Succeeded"));
    Assert.That(result.FilePath, Is.EqualTo(path));
}
```

Add `HandleMessage_MissingMetrics_ReturnsFailedWithoutOpeningDialog` and `HandleMessage_ExporterThrows_ReturnsFailedWithConciseMessage`; assert no exception escapes, no fake dialog call for missing metrics, and no technical stack trace in `ErrorMessage`.

Use these exact fixture seams so the tests remain deterministic and never open Eto:

```csharp
[SetUp]
public void SetUp()
{
    UrbanModel.Instance.Reset();
}

private static ExportExcelMessage CreateMessage(
    IExcelSaveDialog dialog,
    IUrbanMetricsExcelExporter exporter,
    string? projectName,
    DateTime? exportedAt = null)
{
    return new ExportExcelMessage(
        dialog,
        exporter,
        () => projectName,
        () => exportedAt ?? new DateTime(2026, 8, 4, 14, 30, 0));
}

private static void SetCalculatedMetrics()
{
    UrbanModel.Instance.ProjectMetrics = new ProjectMetrics(
        buildingMetrics: [],
        parcelMetrics: [],
        transitMetrics: [],
        zoneMetrics: []);
}

private sealed class FakeSaveDialog(string? result) : IExcelSaveDialog
{
    public int CallCount { get; private set; }
    public string? SuggestedFileName { get; private set; }

    public string? Show(string suggestedFileName)
    {
        CallCount++;
        SuggestedFileName = suggestedFileName;
        return result;
    }
}

private sealed class FakeExporter(Exception? exception = null)
    : IUrbanMetricsExcelExporter
{
    public int CallCount { get; private set; }
    public string? ProjectName { get; private set; }
    public DateTime ExportedAt { get; private set; }

    public void Export(
        UrbanModel urbanModel,
        string filePath,
        string projectName,
        DateTime exportedAt)
    {
        CallCount++;
        ProjectName = projectName;
        ExportedAt = exportedAt;
        if (exception is not null)
            throw exception;
    }
}
```

The failure tests are:

```csharp
[Test]
public void HandleMessage_MissingMetrics_ReturnsFailedWithoutOpeningDialog()
{
    var dialog = new FakeSaveDialog(@"C:\Exports\ignored.xlsx");
    var result = CreateMessage(dialog, new FakeExporter(), "Sample.3dm")
        .HandleMessage();

    Assert.That(result.Status, Is.EqualTo("Failed"));
    Assert.That(result.ErrorMessage, Is.EqualTo("Calculate urban metrics before exporting."));
    Assert.That(dialog.CallCount, Is.Zero);
}

[Test]
public void HandleMessage_ExporterThrows_ReturnsConciseFailedResult()
{
    SetCalculatedMetrics();
    var exporter = new FakeExporter(new IOException("disk details"));
    var result = CreateMessage(
            new FakeSaveDialog(@"C:\Exports\Sample.xlsx"),
            exporter,
            "Sample.3dm")
        .HandleMessage();

    Assert.That(result.Status, Is.EqualTo("Failed"));
    Assert.That(result.ErrorMessage, Is.EqualTo("The Excel export could not be completed."));
    Assert.That(result.ErrorMessage, Does.Not.Contain("disk details"));
}
```

- [ ] **Step 2: Run the tests to verify compilation failure**

Run:

```powershell
dotnet test tests/AIQ.Urban.RhinoPlugin.Tests/AIQ.Urban.RhinoInterface.Tests.csproj --filter "FullyQualifiedName~MessageConverterTests.Convert_ExportExcelMessage|FullyQualifiedName~ExportExcelMessageTests"
```

Expected: compilation fails because the request, result, and dialog types do not exist.

- [ ] **Step 3: Add the typed result**

Create a response that serializes status as a string without adding global JSON enum converters:

```csharp
namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;

public sealed class ExcelExportResultMessage : IMessageToUI
{
    public string Type => "ExcelExportResultMessage";
    public string Status { get; }
    public string? FilePath { get; }
    public string? ErrorMessage { get; }

    private ExcelExportResultMessage(
        string status,
        string? filePath = null,
        string? errorMessage = null)
    {
        Status = status;
        FilePath = filePath;
        ErrorMessage = errorMessage;
    }

    public static ExcelExportResultMessage Succeeded(string filePath) =>
        new("Succeeded", filePath: filePath);

    public static ExcelExportResultMessage Cancelled() =>
        new("Cancelled");

    public static ExcelExportResultMessage Failed(string errorMessage) =>
        new("Failed", errorMessage: errorMessage);
}
```

- [ ] **Step 4: Add the save-dialog seam and Eto implementation**

```csharp
internal interface IExcelSaveDialog
{
    string? Show(string suggestedFileName);
}
```

```csharp
using Eto.Forms;

internal sealed class RhinoExcelSaveDialog : IExcelSaveDialog
{
    public string? Show(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Urban Metrics",
            FileName = suggestedFileName,
        };
        dialog.Filters.Add(new FileFilter("Excel Workbook", ".xlsx"));

        var result = dialog.ShowDialog(Rhino.UI.RhinoEtoApp.MainWindow);
        return result is DialogResult.Ok or DialogResult.Yes
            ? dialog.FileName
            : null;
    }
}
```

The `HttpUiServer` already invokes `HandleMessage()` through `RhinoApp.InvokeOnUiThread`; do not add another UI-thread hop inside the message.

- [ ] **Step 5: Implement `ExportExcelMessage`**

Use a public default constructor and one internal injection constructor:

```csharp
public sealed class ExportExcelMessage
    : IMessageFromUI<ExcelExportResultMessage>
{
    private readonly IExcelSaveDialog _dialog;
    private readonly IUrbanMetricsExcelExporter _exporter;
    private readonly Func<string?> _activeDocumentName;
    private readonly Func<DateTime> _clock;

    public TypeOfMessage MessageType => TypeOfMessage.ExportExcel;

    public ExportExcelMessage()
        : this(
            new RhinoExcelSaveDialog(),
            new UrbanMetricsExcelExporter(),
            () => RhinoDoc.ActiveDoc?.Name,
            () => DateTime.Now)
    {
    }

    internal ExportExcelMessage(
        IExcelSaveDialog dialog,
        IUrbanMetricsExcelExporter exporter,
        Func<string?> activeDocumentName,
        Func<DateTime> clock)
    {
        _dialog = dialog;
        _exporter = exporter;
        _activeDocumentName = activeDocumentName;
        _clock = clock;
    }
}
```

`HandleMessage` follows this exact order:

```csharp
public ExcelExportResultMessage HandleMessage()
{
    if (UrbanModel.Instance.ProjectMetrics is null)
    {
        return ExcelExportResultMessage.Failed(
            "Calculate urban metrics before exporting.");
    }

    var projectName = Path.GetFileNameWithoutExtension(
        _activeDocumentName() ?? string.Empty);
    var suggestedFileName = string.IsNullOrWhiteSpace(projectName)
        ? "Urban Metrics.xlsx"
        : $"{projectName} - Urban Metrics.xlsx";
    var filePath = _dialog.Show(suggestedFileName);

    if (filePath is null)
        return ExcelExportResultMessage.Cancelled();

    try
    {
        _exporter.Export(
            UrbanModel.Instance,
            filePath,
            string.IsNullOrWhiteSpace(projectName) ? "Urban Metrics" : projectName,
            _clock());
        return ExcelExportResultMessage.Succeeded(filePath);
    }
    catch (Exception exception)
    {
        RhinoApp.WriteLine($"AIQ Urban Excel export failed: {exception}");
        return ExcelExportResultMessage.Failed(
            "The Excel export could not be completed.");
    }
}
```

- [ ] **Step 6: Route the request**

Add `ExportExcel` to `TypeOfMessage` and this converter arm:

```csharp
"exportexcel" => new ExportExcelMessage(),
```

- [ ] **Step 7: Run orchestration tests and commit**

Run:

```powershell
dotnet test tests/AIQ.Urban.RhinoPlugin.Tests/AIQ.Urban.RhinoInterface.Tests.csproj --filter "FullyQualifiedName~MessageConverterTests|FullyQualifiedName~ExportExcelMessageTests"
```

Expected: all converter and export message tests pass.

```powershell
git add src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/IExcelSaveDialog.cs src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/RhinoExcelSaveDialog.cs src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/ExportExcelMessage.cs src/AIQ.Urban.Rhino/HTML_UI/MessageToUI/ExcelExportResultMessage.cs src/AIQ.Urban.Rhino/HTML_UI/MessagesFromUI/IMessageFromUI.cs src/AIQ.Urban.Rhino/HTML_UI/MessageConverter.cs tests/AIQ.Urban.RhinoPlugin.Tests/HTML_UI/MessageConverterTests.cs tests/AIQ.Urban.RhinoPlugin.Tests/HTML_UI/ExportExcelMessageTests.cs
git commit -m "feat: handle Excel export requests in Rhino"
```

---

### Task 6: Replace the Vue path picker with one export action

**Repository:** `C:\Users\AshwinAbraham\source\repos\aiqurban-ui`

**Files:**
- Create: `src/components/MESSAGES/ExcelExportResultMessage.ts`
- Modify: `src/services/RhinoApiService.ts`
- Rewrite: `src/components/EXPORTS/PluginExports.vue`

**Interfaces:**
- Consumes: `ExcelExportResultMessage` JSON from Task 5.
- Produces: one `{ messageType: 'ExportExcel' }` POST with disabled/exporting and success/cancel/failure feedback.

- [ ] **Step 1: Add the frontend response type**

```typescript
export type ExcelExportStatus = 'Succeeded' | 'Cancelled' | 'Failed'

export class ExcelExportResultMessage {
  constructor(
    public Status: ExcelExportStatus,
    public FilePath: string | null,
    public ErrorMessage: string | null,
  ) {}
}
```

- [ ] **Step 2: Decode the new Rhino message**

In `RhinoApiService.ts`, import the class, add `'ExcelExportResultMessage'` to `RhinoMessageType`, add the class to `RhinoMessage`, and register:

```typescript
ExcelExportResultMessage: handleExcelExportResultMessage,
```

Implement the handler against the camel-case HTTP JSON contract:

```typescript
function handleExcelExportResultMessage(raw: Record<string, unknown>) {
  const data = raw as unknown as {
    status: 'Succeeded' | 'Cancelled' | 'Failed'
    filePath: string | null
    errorMessage: string | null
  }

  return new ExcelExportResultMessage(data.status, data.filePath, data.errorMessage)
}
```

- [ ] **Step 3: Rewrite `PluginExports.vue` around the shared service**

Remove `DirectoryPickedMessage`, `filePickedMessage`, `filePath`, the `Location` button, and manual `JSON.stringify`. Use this state and method:

```javascript
import { ExcelExportResultMessage } from '../MESSAGES/ExcelExportResultMessage'
import { RhinoApiService } from '../../services/RhinoApiService'

export default {
  data() {
    return {
      isExporting: false,
      feedback: '',
      feedbackKind: '',
    }
  },
  methods: {
    async exportExcel() {
      if (this.isExporting) return

      this.isExporting = true
      this.feedback = ''
      this.feedbackKind = ''

      try {
        const result = await RhinoApiService.send({ messageType: 'ExportExcel' })
        if (!(result instanceof ExcelExportResultMessage)) {
          this.feedback = 'The Excel export could not be completed.'
          this.feedbackKind = 'error'
          return
        }

        if (result.Status === 'Succeeded') {
          this.feedback = `Saved to ${result.FilePath}`
          this.feedbackKind = 'success'
        } else if (result.Status === 'Failed') {
          this.feedback = result.ErrorMessage ?? 'The Excel export could not be completed.'
          this.feedbackKind = 'error'
        }
      } finally {
        this.isExporting = false
      }
    },
  },
}
```

The template contains one button:

```html
<button class="app-button" type="button" :disabled="isExporting" @click="exportExcel">
  {{ isExporting ? 'Exporting…' : 'Export Excel' }}
</button>
<p v-if="feedback" class="export-feedback" :class="feedbackKind" aria-live="polite">
  {{ feedback }}
</p>
```

Cancellation leaves `feedback` empty and returns the button to idle. Add scoped `.success`, `.error`, and disabled-button styles using the existing CSS variables; do not add a modal or auto-open behavior.

- [ ] **Step 4: Format and build the UI**

Run from `C:\Users\AshwinAbraham\source\repos\aiqurban-ui`:

```powershell
npx prettier --write src/components/MESSAGES/ExcelExportResultMessage.ts src/services/RhinoApiService.ts src/components/EXPORTS/PluginExports.vue
npm run build
```

Expected: Vite completes without TypeScript or Vue compilation errors and refreshes `dist`.

- [ ] **Step 5: Commit the UI change in the UI repository**

```powershell
git add src/components/MESSAGES/ExcelExportResultMessage.ts src/services/RhinoApiService.ts src/components/EXPORTS/PluginExports.vue
git commit -m "feat: trigger urban metrics Excel export"
```

Do not stage unrelated UI files. Stage `dist` only if it is already tracked by the UI repository and `git status --short` reports build artifacts as modified.

---

### Task 7: Verify the complete export and refresh the knowledge graph

**Files:**
- Modify when generated: `graphify-out/**`
- Verify: both repositories' clean intended diffs and built artifacts.

**Interfaces:**
- Consumes: all earlier tasks.
- Produces: a verified plugin/UI build and current code knowledge graph.

- [ ] **Step 1: Run the full focused .NET test set**

From the ArchSitePlan repository, run:

```powershell
dotnet test tests/AIQ.Exports.Tests/AIQ.Exports.Tests.csproj
dotnet test tests/AIQ.Urban.Core.Tests/AIQ.Urban.Core.Tests.csproj --filter "FullyQualifiedName~ProjectMetricsCalculatorTests"
dotnet test tests/AIQ.Urban.RhinoPlugin.Tests/AIQ.Urban.RhinoInterface.Tests.csproj --filter "FullyQualifiedName~UrbanMetricsExcelExporterTests|FullyQualifiedName~ExportExcelMessageTests|FullyQualifiedName~MessageConverterTests"
```

Expected: all commands exit `0` with no failed tests.

- [ ] **Step 2: Build the plugin against the freshly built Vue UI**

Run:

```powershell
dotnet build src/AIQ.Urban.Rhino/AIQ.Urban.RhinoPlugin.csproj -c Debug
```

Expected: build succeeds and the linked `aiqurban-ui/dist/index.html` is present.

- [ ] **Step 3: Perform the manual Rhino/UI acceptance check**

Load the built plugin in Rhino with a model whose metrics have been calculated, then verify these exact behaviors:

1. Exports shows one `Export Excel` button and no Location button.
2. Clicking once disables the action and opens the native `.xlsx` Save dialog.
3. Cancel returns to idle with no error text and creates no file.
4. Confirm creates the chosen workbook and shows `Saved to <path>` without opening Excel.
5. Project Overview and each Zone sheet match the accepted tables; an empty Zone shows `No data` in all three detail tables.
6. A model without calculated metrics shows `Calculate urban metrics before exporting.` without opening the Save dialog.
7. Overwriting an existing workbook prompts in the native dialog and leaves a valid workbook.

- [ ] **Step 4: Refresh graphify and inspect changes**

Run:

```powershell
graphify update .
git status --short
git diff --check
```

Expected: graph update completes, `git diff --check` reports no whitespace errors, and status contains only intended source/test/graph changes.

- [ ] **Step 5: Commit generated graph updates if present**

If `git status --short graphify-out` is empty, do not create a commit. Otherwise run:

```powershell
git add graphify-out
git commit -m "chore: refresh code knowledge graph"
```

- [ ] **Step 6: Record final evidence**

Capture in the implementation handoff:

- the three `dotnet test` summaries;
- the `dotnet build` summary;
- the `npm run build` summary;
- the manual Rhino/UI checklist result;
- the ArchSitePlan and `aiqurban-ui` commit hashes;
- any uncommitted files in either repository.

Do not claim completion if any required test, build, or manual acceptance item is unverified.
