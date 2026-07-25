# Implement AIQ.Exports Excel Foundation

Status: Completed

Architecture: [AIQ.Exports Excel Architecture](./aiq-exports-excel-architecture.md)

## Objective

Implement the domain-neutral Excel foundation described in the architecture document inside `AIQ.Exports`, with focused xUnit coverage and a generated workbook that can be opened and visually reviewed.

This plan does not add Urban-specific worksheet composition or references between `AIQ.Exports` and any `AIQ.Urban` project.

## Delivery boundaries

### In scope

- Detached Excel cell content and style models.
- Rectangular mutable `TableBlock` instances with fixed dimensions after `Build()`.
- Incremental `TableBlockBuilder` row and column operations.
- One-based worksheet addresses.
- Vertical block placement with a configurable gap.
- Worksheet and document definitions.
- ClosedXML-backed table-block and document writing.
- Focused unit and integration tests.
- One inspectable example `.xlsx` produced by the test suite.

### Out of scope

- Urban metrics and orchestration.
- Native Excel tables.
- Charts, images, merged cells, borders, conditional formatting, and templates.
- Editing existing workbooks.
- Additional placement strategies.
- Automatic destination-directory creation.

## Test seams to confirm

The implementation will test behavior only through these public seams:

1. `TableBlockBuilder`
   - Callers add rows, modify columns, apply row styles, and call `Build()`.
   - Tests inspect the returned `TableBlock` through its public cell matrix, dimensions, and cell styles.

2. `VerticalBlockPlacementStrategy`
   - Callers pass blocks and a starting worksheet address.
   - Tests inspect each block's assigned `SeedCellAddress`.

3. `ExcelDocumentWriter`
   - Callers provide an `ExcelDocument` and destination path.
   - Tests reopen the resulting `.xlsx` file and verify observable worksheet content, formulas, and representative styles.

No internal writer, renderer, or ClosedXML call-order tests are planned. No mocks are required.

## TDD workflow

Each implementation slice follows:

```text
one failing behavior test
    → minimal production implementation
    → targeted test passes
    → next behavior test
```

Tests are not written as one large batch. Each slice should be completed before the next slice begins.

After all red-green slices pass, perform a separate review for naming, duplication, and API clarity.

## Proposed production structure

```text
src/AIQ.Exports/
└── Excel/
    ├── Models/
    │   ├── ExcelCell.cs
    │   ├── ExcelCellAddress.cs
    │   ├── ExcelCellContent.cs
    │   ├── ExcelCellStyle.cs
    │   ├── ExcelDocument.cs
    │   ├── ExcelFormulaNotation.cs
    │   ├── ExcelHorizontalAlignment.cs
    │   ├── ExcelVerticalAlignment.cs
    │   ├── ExcelWorksheetDefinition.cs
    │   ├── IExcelBlock.cs
    │   └── TableBlock.cs
    ├── Builders/
    │   └── TableBlockBuilder.cs
    ├── Placement/
    │   ├── IExcelBlockPlacementStrategy.cs
    │   └── VerticalBlockPlacementStrategy.cs
    └── Writing/
        ├── ExcelDocumentWriter.cs
        ├── IExcelBlockWriter.cs
        └── TableBlockWriter.cs
```

The exact number of files may be reduced where a small enum naturally belongs beside its model. Public types should remain one per file unless combining them materially improves readability.

## Proposed test structure

```text
tests/AIQ.Exports.Tests/
└── Excel/
    ├── ExcelCellTests.cs
    ├── TableBlockBuilderTests.cs
    ├── VerticalBlockPlacementStrategyTests.cs
    ├── ExcelDocumentWriterTests.cs
    └── ExcelExampleWorkbookTests.cs
```

Test names follow:

```text
MethodName_Scenario_ExpectedBehavior
```

Use `[Theory]` where several scalar types exercise the same behavior. Use `[Fact]` where the scenario has distinct setup or assertions.

## Slice 1 — Detached cells

### Red

Add tests through the `ExcelCell` factories:

1. `From_SupportedScalar_PreservesTypedValue`
2. `From_UnsupportedObject_ThrowsArgumentException`
3. `Blank_CreatesBlankContent`
4. `FormulaA1_Expression_CreatesA1Formula`
5. `FormulaR1C1_Expression_CreatesR1C1Formula`

Use known literal values rather than recomputing expected results.

### Green

Implement:

- `ExcelFormulaNotation`
- `ExcelCellContent`
- `ExcelCell`
- Minimal `ExcelCellStyle`

Supported scalar types:

- `string`
- integral numeric CLR types
- `float`, `double`, and `decimal`
- `bool`
- `DateTime`
- `TimeSpan`

Formula factories reject null or whitespace expressions. They store expressions without requiring a leading equals sign.

### Exit condition

All detached-cell tests pass without referencing ClosedXML types in the public model.

## Slice 2 — Rows produce a rectangular block

### Red

Add one behavior test:

`Build_RowsHaveDifferentLengths_UsesLongestRowAndFillsBlanks`

Worked example:

```text
["A", "B", "C"]
["D"]
```

Expected block:

```text
2 rows × 3 columns
"A" "B" "C"
"D" blank blank
```

### Green

Implement the minimum:

- `IExcelBlock`
- `TableBlock`
- `TableBlockBuilder.AddNewRow`
- `TableBlockBuilder.Build`

Builder storage remains expandable until `Build()`. `Build()` materialises a new rectangular `ExcelCell[,]`.

The returned block:

- Has fixed dimensions.
- Allows callers to replace cells within the existing matrix.
- Starts with `SeedCellAddress = null`.

### Exit condition

The worked example passes through the public builder and block APIs.

## Slice 3 — Structural build boundary

### Red

Add:

`Build_BuilderChangesAfterFirstBuild_DoNotResizePreviousBlock`

Worked sequence:

1. Add one two-cell row.
2. Build `first`.
3. Add a second three-cell row.
4. Build `second`.

Expected:

- `first` remains `1 × 2`.
- `second` is `2 × 3`.

Add:

`Cells_ReplaceCellAfterBuild_DimensionsRemainFixed`

### Green

Ensure each `Build()` creates a new cell matrix from current builder state. Do not expose structural resize operations on `TableBlock`.

### Exit condition

Block mutation and structural independence are both demonstrated through public behavior.

## Slice 4 — Column modification and collision behavior

### Red

Add one test per distinct behavior:

1. `ModifyColumn_DataLongerThanRows_ExpandsNextBuiltBlock`
2. `ModifyColumn_LaterColumn_LeavesIntermediateCellsBlank`
3. `ModifyColumn_OverwriteDisabled_PreservesPopulatedCells`
4. `ModifyColumn_OverwriteDisabled_WritesStyledBlankCells`
5. `ModifyColumn_OverwriteEnabled_ReplacesExistingContent`

Use small literal grids for expected results.

### Green

Implement `ModifyColumn` with zero-based `columnIndex`.

When overwrite is disabled:

- Write into blank cells.
- Preserve scalar and formula cells.
- Treat a styled blank as blank.
- Apply the operation style only where content is written.

When overwrite is enabled:

- Replace content.
- Merge the operation style according to the shared style rules.

Reject negative indices with `ArgumentOutOfRangeException`.

### Exit condition

Column extension, blank handling, and overwrite semantics pass without inspecting builder internals.

## Slice 5 — Style composition

### Red

Add:

1. `AddNewRow_RowAndCellStyles_CellPropertiesWin`
2. `SetRowStyle_PartialStyle_PreservesUnspecifiedProperties`
3. `ModifyColumn_StyleAppliedLater_SpecifiedPropertiesWin`
4. `SetRowStyle_RowDoesNotExist_ThrowsArgumentOutOfRangeException`

Use a worked style example:

- Row style supplies bold and orange fill.
- Cell style supplies a percentage number format.
- Later column style supplies horizontal alignment.
- Final style contains all four properties.

### Green

Implement:

- `ExcelCellStyle.Merge`
- `TableBlockBuilder.SetRowStyle`
- Style application within `AddNewRow`
- Style application within `ModifyColumn`

Composition rules:

- Incoming non-null properties replace existing properties.
- Incoming null properties preserve existing properties.
- Row style is applied before a newly added cell's explicit style.
- Later builder operations merge over the resolved cell style.

### Exit condition

Style precedence is observable through final cell styles and does not depend on persistent row or column style objects.

## Slice 6 — Worksheet addressing and vertical placement

### Red

Add:

1. `Constructor_NonPositiveCoordinate_ThrowsArgumentOutOfRangeException`
2. `Place_Blocks_AssignsAddressesWithConfiguredGap`
3. `Place_EmptyBlocks_PerformsNoWork`
4. `Constructor_NegativeGap_ThrowsArgumentOutOfRangeException`
5. `Place_BlockWouldExceedWorksheetBounds_ThrowsArgumentOutOfRangeException`

Worked placement:

```text
Starting address: row 2, column 3
Gap: 2 rows
First block height: 3
Second block height: 4
```

Expected seeds:

```text
First:  row 2, column 3
Second: row 7, column 3
```

### Green

Implement:

- `ExcelCellAddress`
- `IExcelBlockPlacementStrategy`
- `VerticalBlockPlacementStrategy`

Use Excel worksheet limits:

- Maximum row: 1,048,576
- Maximum column: 16,384

The strategy mutates each block's `SeedCellAddress`.

### Exit condition

Placement is correct at the zero-based block/one-based worksheet boundary and rejects invalid worksheet positions.

## Slice 7 — Minimal workbook writing

### Red

Add:

`Write_SingleWorksheetAndBlock_CreatesExpectedWorkbook`

The test:

1. Builds a two-row block.
2. Creates one worksheet definition with vertical placement starting at `B2`.
3. Creates an `ExcelDocument`.
4. Writes to a unique temporary `.xlsx` path.
5. Reopens the file through ClosedXML.
6. Asserts the sheet name and exact values at `B2:C3`.

Delete the temporary file in test cleanup.

### Green

Implement:

- `ExcelWorksheetDefinition`
- `ExcelDocument`
- `IExcelBlockWriter`
- `TableBlockWriter`
- `ExcelDocumentWriter`

The document writer:

- Requires at least one worksheet.
- Rejects invalid or duplicate worksheet names.
- Requires the destination directory to exist.
- Executes each worksheet's placement strategy.
- Selects a compatible block writer.
- Saves the `XLWorkbook`.

The table block writer:

- Requires `SeedCellAddress`.
- Writes supported scalar values.
- Leaves unstyled blanks unmaterialised.

### Exit condition

A caller can produce and reopen a minimal workbook using only public `AIQ.Exports` models and `ExcelDocumentWriter`.

## Slice 8 — Formulas and styles survive writing

### Red

Add:

1. `Write_A1AndR1C1Formulas_PreservesNotationAndExpressions`
2. `Write_StyledCells_AppliesRepresentativeClosedXmlStyles`

Verify representative style behavior:

- Bold font.
- Fill colour.
- Number format.
- Horizontal alignment.
- Wrapped text.

### Green

Extend `TableBlockWriter` to:

- Assign `FormulaA1` or `FormulaR1C1` from `ExcelFormulaNotation`.
- Translate supported `ExcelCellStyle` properties to ClosedXML.
- Materialise styled blank cells only when required.

### Exit condition

The reopened workbook contains the expected formulas and representative formatting.

## Slice 9 — Multiple blocks and worksheets

### Red

Add:

`Write_MultipleWorksheetsAndBlocks_UsesEachWorksheetLayout`

Worked document:

- First worksheet starts at `A1`, vertical gap 2, two blocks.
- Second worksheet starts at `C3`, vertical gap 1, one block.

Assert exact seed-derived output cells and blank gap rows after reopening the workbook.

### Green

Make only the writer changes required by the test. Avoid adding layout configuration outside `ExcelWorksheetDefinition`.

### Exit condition

One document successfully applies independent per-worksheet placement choices.

## Slice 10 — Inspectable example workbook

### Red

Add:

`Write_ExampleWorkbook_CreatesInspectableOutput`

This is a behavioral integration test, not a snapshot test. It must assert representative cells, formulas, and styles before reporting the file path.

### Green

Create a compact example containing:

- Two worksheets.
- Two vertically arranged blocks on the first worksheet.
- Uneven input rows.
- A column modification.
- Header, body, percentage, and total styles.
- Both A1 and R1C1 formulas.
- A visible inter-block gap.

Write to:

```text
<test assembly directory>/TestOutputs/AIQ.Exports.Excel.Example.xlsx
```

Use `ITestOutputHelper` to print the absolute path.

The file is deliberately retained after the test for manual review.

### Exit condition

The test passes, the file exists at the reported location, and key workbook content has already been verified programmatically.

## Visual verification

After the example workbook is generated:

1. Render or open every worksheet.
2. Check that blocks appear at their expected addresses.
3. Confirm the inter-block gaps.
4. Check that headers and values are not clipped.
5. Confirm percentage and number formats.
6. Confirm fills and font emphasis are legible.
7. Scan for formula errors.
8. Make only focused styling or writer corrections required by the review.

Visual verification is required before the foundation is considered complete.

## Existing code handling

`src/AIQ.Exports/OpenXMLFile.cs` is an unused placeholder around `XLWorkbook`.

Remove it only after the minimal workbook-writing slice passes and `ExcelDocumentWriter` replaces its intended role. No other PowerPoint or Google Slides export code should be modified unless compilation requires a narrowly scoped compatibility change.

## Verification commands

Run targeted tests during each slice:

```powershell
dotnet test tests/AIQ.Exports.Tests/AIQ.Exports.Tests.csproj `
    --filter "FullyQualifiedName~AIQ.Exports.Tests.Excel"
```

After all slices:

```powershell
dotnet test tests/AIQ.Exports.Tests/AIQ.Exports.Tests.csproj
dotnet build ArchSitePlan.sln -c Debug_NoViz
```

If the full solution build has unrelated pre-existing failures, record them separately and still report the targeted project/test results.

## Completion criteria

- All agreed public seams have focused tests.
- Targeted `AIQ.Exports.Tests` tests pass.
- The broader `AIQ.Exports.Tests` run passes, or unrelated legacy/integration failures are recorded separately.
- `AIQ.Exports` builds without warnings introduced by this work.
- The inspectable workbook is generated at a reported absolute path.
- Every generated worksheet receives a visual verification pass.
- No Urban project is modified.
- The architecture document is updated if implementation reveals a necessary design correction.

## Implementation result

Completed on 25 July 2026.

- Added the detached Excel cell, style, formula, address, block, worksheet, and document models.
- Added `TableBlockBuilder` with uneven-row expansion, column modification, row styling, and independent builds.
- Added mutable block placement through `SeedCellAddress` and `VerticalBlockPlacementStrategy`.
- Added ClosedXML-backed table and document writers without exposing ClosedXML through the public model.
- Added focused unit and integration coverage, including an inspectable two-worksheet workbook.
- Added content-based column sizing after visual review showed that default widths clipped representative values.
- Removed the unused `OpenXMLFile` placeholder after `ExcelDocumentWriter` replaced its intended role.
- Left all Urban projects unchanged.

Verification:

- Focused Excel suite: 31 passed, 0 failed.
- `AIQ.Exports` build: succeeded with no errors and one pre-existing NuGet version-resolution warning.
- Full `Debug_NoViz` solution build: succeeded with 0 errors.
- The example workbook was rendered worksheet-by-worksheet; placement, spacing, sizing, formatting, and formula-error checks passed.
- The broader `AIQ.Exports.Tests` run has 43 passing tests and 10 unrelated legacy/integration failures: one missing PowerPoint template and nine Google Slides tests requiring unavailable external connectivity or credentials. These failures were not introduced by the Excel foundation.
