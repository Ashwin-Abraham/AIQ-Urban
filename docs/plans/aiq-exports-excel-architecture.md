# AIQ.Exports Excel Architecture

Status: Accepted for implementation.

## Purpose

Create a reusable Excel-export foundation inside `AIQ.Exports`.

The foundation accepts an in-memory description of worksheet content, places that content on worksheets, and writes an `.xlsx` file using ClosedXML. A consumer should not need to work with ClosedXML workbooks, worksheets, or cells directly.

This document deliberately describes only the `AIQ.Exports` foundation and its tests. Urban metrics, `AIQ.Urban.Core`, and `AIQ.Urban.RhinoInterface` orchestration are deferred until this foundation is agreed and working.

## Goals

- Represent worksheet content independently from a live Excel workbook.
- Support rectangular blocks of styled cells.
- Build blocks incrementally from rows and columns of different lengths.
- Calculate the final rectangular block dimensions during `Build()`.
- Separate content construction, worksheet placement, and ClosedXML output.
- Allow different block-placement strategies.
- Keep the initial API small while leaving clear extension points.
- Provide a small set of tests that generate workbooks which can be opened and reviewed.

## Non-goals for the first iteration

- Urban-specific metrics or workbook composition.
- Charts, images, pivot tables, or conditional formatting.
- A complete abstraction over every Excel feature.
- Reading or editing existing workbooks.
- Supporting spreadsheet libraries other than ClosedXML.
- Automatic formula generation from domain data.
- A general report templating language.

## Proposed pipeline

```text
Consumer data
    |
    v
TableBlockBuilder
    |
    v
Built IExcelBlock instances
    |
    v
IExcelBlockPlacementStrategy
    |
    v
Blocks with assigned SeedCellAddress
    |
    v
ExcelBlockWriter
    |
    v
ExcelDocumentWriter
    |
    v
.xlsx file
```

Each stage has one responsibility:

- The builder constructs cell content.
- A block stores completed content and its dimensions.
- A placement strategy assigns worksheet locations.
- A block writer translates placed cells to ClosedXML.
- The document writer owns workbook and worksheet creation and file saving.

## Core model

### ExcelCell

`ExcelCell` is a detached description of one future worksheet cell.

ClosedXML's `IXLCell` should not be used in the intermediate model because it belongs to a live worksheet and combines value, address, formula, and workbook-backed style. `XLCellValue` can represent a detached scalar value, but it does not represent the cell's formula and style. Exposing it would also require consumers of `AIQ.Exports` to construct ClosedXML types.

The proposed model therefore uses an `AIQ.Exports` cell type:

```csharp
public sealed record ExcelCell(
    ExcelCellContent Content,
    ExcelCellStyle? Style = null);
```

Cell content is either blank, a scalar value, or a formula:

```csharp
public abstract record ExcelCellContent
{
    public sealed record Blank : ExcelCellContent;
    public sealed record Value(object Data) : ExcelCellContent;
    public sealed record Formula(
        string Expression,
        ExcelFormulaNotation Notation) : ExcelCellContent;
}
```

The `Value` case accepts a deliberately limited set of scalar types:

- `string`
- numeric CLR types
- `bool`
- `DateTime`
- `TimeSpan`

Unsupported values fail during construction rather than much later during file writing.

Convenience factories should cover normal use:

```csharp
ExcelCell.Blank();
ExcelCell.From("Residential");
ExcelCell.From(12_500.0);
ExcelCell.FormulaA1("SUM(C4:C10)");
ExcelCell.FormulaR1C1("SUM(R[-6]C:R[-1]C)");
ExcelCell.From(0.42, percentageStyle);
```

Both A1 and R1C1 formulas are supported explicitly. There is no notation default: callers use `FormulaA1` or `FormulaR1C1`, and the content records the corresponding `ExcelFormulaNotation`.

Formula expressions are stored without requiring a leading `=`. The ClosedXML writer assigns the expression to `FormulaA1` or `FormulaR1C1` according to its notation.

References:

- [ClosedXML cell API](https://docs.closedxml.io/en/latest/api/cell.html)
- [ClosedXML 0.105 formula migration notes](https://docs.closedxml.io/en/latest/migrations/migrate-to-0.105.html)

### ExcelCellStyle

Styles are detached, immutable data:

```csharp
public sealed record ExcelCellStyle(
    bool? Bold = null,
    double? FontSize = null,
    Color? FontColor = null,
    Color? FillColor = null,
    string? NumberFormat = null,
    ExcelHorizontalAlignment? HorizontalAlignment = null,
    ExcelVerticalAlignment? VerticalAlignment = null,
    bool? WrapText = null);
```

Nullable properties mean "unspecified", allowing styles to be composed without requiring every style to repeat every setting.

The initial implementation should include only style properties needed by the sample workbooks. Borders, merged cells, and other features can be added when a concrete use case requires them.

## Blocks

### IExcelBlock

A block is a completed rectangular unit of worksheet content:

```csharp
public interface IExcelBlock
{
    int RowCount { get; }
    int ColumnCount { get; }
    ExcelCellAddress? SeedCellAddress { get; set; }
}
```

The first implementation is `TableBlock`. The interface exists so future blocks can provide specialised rendering or behaviour without changing placement and document orchestration.

`SeedCellAddress` is `null` while the block is unplaced. A placement strategy assigns it before the writing stage. A block writer rejects a block whose seed address has not been assigned.

### TableBlock

`TableBlock` stores a rectangular two-dimensional array:

```csharp
public sealed class TableBlock : IExcelBlock
{
    public ExcelCell[,] Cells { get; private set; }

    public int RowCount => Cells.GetLength(0);
    public int ColumnCount => Cells.GetLength(1);
    public ExcelCellAddress? SeedCellAddress { get; set; }
}
```

The block remains mutable after construction. A builder or consumer may replace cell content and styles after the initial rectangular matrix has been materialised.

`Build()` is the structural boundary:

- Before `Build()`, the builder may add rows, extend columns, and change the eventual dimensions.
- After `Build()`, individual cells may be replaced within the existing bounds.
- A built block's `RowCount` and `ColumnCount` remain fixed.
- Further structural changes to the builder are reflected by calling `Build()` again, which returns a newly sized block without resizing a previously returned block.

Stable built dimensions allow placement strategies to calculate subsequent block positions without later structural mutations causing overlaps.

`TableBlock` means a rectangular block of cells. It does not initially imply creation of an Excel native table with structured references, filters, or a named table range.

## TableBlockBuilder

The builder supports incremental construction without repeatedly resizing a rectangular array.

Internally it may use expandable rows:

```csharp
List<List<ExcelCell?>>
```

Only `Build()` materialises the final `ExcelCell[,]`.

### Proposed API

```csharp
public sealed class TableBlockBuilder
{
    public TableBlockBuilder AddNewRow(
        IEnumerable<ExcelCell> data,
        ExcelCellStyle? style = null);

    public TableBlockBuilder ModifyColumn(
        int columnIndex,
        IEnumerable<ExcelCell> data,
        bool overwriteExistingData,
        ExcelCellStyle? style = null);

    public TableBlockBuilder SetRowStyle(
        int rowIndex,
        ExcelCellStyle style);

    public TableBlock Build();
}
```

The method name `SetRowStyle` is proposed instead of `SetStyle` so the target of the operation remains explicit when column and cell styling are added.

### AddNewRow

`AddNewRow` appends a row.

- Rows may have different lengths.
- A supplied row style is applied to all cells in that row.
- Missing cells are added as blanks when the final matrix is materialised.
- An empty row is valid and affects the final row count but not the column count.

Example:

```csharp
builder
    .AddNewRow(
        [ExcelCell.From("Name"), ExcelCell.From("Value")],
        headingStyle)
    .AddNewRow(
        [ExcelCell.From("Area"), ExcelCell.From(1_250.0)]);
```

### ModifyColumn

`ModifyColumn` writes data vertically beginning at the first row.

- If the supplied data is longer than the current row count, the builder creates additional rows.
- If the column is beyond the current width, intermediate cells remain blank.
- `overwriteExistingData: true` replaces existing content.
- The optional style applies to cells written by this operation.

When `overwriteExistingData` is `false`:

- Data is written into blank cells.
- Cells that already contain a scalar value or formula are preserved.
- The supplied style is applied only to cells whose data is written.
- A styled-but-blank cell still counts as blank and may receive data.
- Encountering a populated cell does not throw.

When `overwriteExistingData` is `true`, the supplied content replaces existing content and the optional style participates in the normal style-composition rules.

Possible later overloads could accept a starting row:

```csharp
ModifyColumn(
    int columnIndex,
    int startingRowIndex,
    IEnumerable<ExcelCell> data,
    bool overwriteExistingData,
    ExcelCellStyle? style = null);
```

The first iteration should add this parameter only if a test or sample requires partial-column updates.

### SetRowStyle

`SetRowStyle` applies a style to an existing row.

An attempt to style a row that does not yet exist should fail immediately with an `ArgumentOutOfRangeException`; styling should not silently create content.

Each `ExcelCell` stores its resolved style rather than references to persistent row-style or column-style layers.

Styles are merged property-by-property:

- A non-null property in the incoming style replaces the corresponding existing property.
- A null property in the incoming style preserves the existing property.
- Later builder operations therefore win only for the properties they specify.
- During `AddNewRow(data, rowStyle)`, the row style is applied first and each cell's explicit style is applied second.
- Later calls to `SetRowStyle` or `ModifyColumn(..., style)` merge their styles over the cells they affect.

For example, applying a percentage number format to one column of a bold, filled heading row changes the number format without removing the existing bold font or fill.

### Build

`Build()`:

1. Finds the greatest row width.
2. Uses the total number of accumulated rows as the block height.
3. Allocates one `ExcelCell[,]`.
4. Copies the accumulated cells into the matrix.
5. Populates all unspecified positions with `ExcelCell.Blank()`.
6. Returns a mutable `TableBlock`.

Calling `Build()` more than once produces a new block from the builder's current structural state. Previously returned blocks retain their original dimensions and cell matrix.

## Addressing

Worksheet addresses should be represented by an `AIQ.Exports` value type rather than a ClosedXML address:

```csharp
public readonly record struct ExcelCellAddress(int Row, int Column);
```

The type should validate Excel's positive, one-based row and column coordinates.

Using a structured address avoids passing strings such as `"A1"` throughout placement logic. Conversion to A1 notation, if needed, happens inside the ClosedXML writer.

Builder row and column indices are zero-based, matching normal .NET collection conventions and the `ExcelCell[,]` representation. Parameter names use `rowIndex` and `columnIndex` to make that convention explicit.

`ExcelCellAddress` is one-based, matching worksheet coordinates. Its properties are named `Row` and `Column`.

For example:

```csharp
builder.ModifyColumn(columnIndex: 0, ...); // First block column.
new ExcelCellAddress(Row: 1, Column: 1);   // Worksheet cell A1.
```

The boundary between zero-based block indices and one-based worksheet addresses must be covered explicitly by placement and writer tests.

## Placement

### Block placement state

Placement is stored directly on each mutable `IExcelBlock` through `SeedCellAddress`.

An unplaced block has a `null` seed address. A placement strategy mutates each block to assign its top-left worksheet address. One block instance therefore represents one placement at a time. Reusing the same content at two locations requires two block instances.

This avoids a separate placement wrapper and keeps the writing pipeline straightforward.

An alternative under consideration is placing a mutable `SeedCellAddress` directly on `IExcelBlock`. That is simpler to navigate but couples content to one placement and makes block reuse more difficult.

### IExcelBlockPlacementStrategy

```csharp
public interface IExcelBlockPlacementStrategy
{
    void Place(
        IReadOnlyList<IExcelBlock> blocks,
        ExcelCellAddress startingAddress);
}
```

### VerticalBlockPlacementStrategy

The first strategy places blocks from top to bottom:

```csharp
public sealed class VerticalBlockPlacementStrategy(
    int rowGap) : IExcelBlockPlacementStrategy;
```

Given a starting row `R`, a block height `H`, and a configured gap `G`, the next block starts at:

```text
R + H + G
```

The starting column remains unchanged.

The strategy validates that `rowGap` is non-negative and that the resulting addresses remain within Excel worksheet limits.

## Worksheet and document writing

### ExcelWorksheetDefinition

A worksheet definition contains its name and unplaced blocks:

```csharp
public sealed record ExcelWorksheetDefinition(
    string Name,
    IReadOnlyList<IExcelBlock> Blocks,
    IExcelBlockPlacementStrategy PlacementStrategy,
    ExcelCellAddress StartingAddress);
```

Each worksheet explicitly owns its placement strategy and starting address. The document writer performs no layout selection; it executes the layout declared by each worksheet. This allows different worksheets in one document to use different placement strategies later without configuring multiple document writers.

### ExcelDocument

```csharp
public sealed record ExcelDocument(
    IReadOnlyList<ExcelWorksheetDefinition> Worksheets);
```

The document contains content and layout choices but no file path and no ClosedXML workbook.

### ExcelBlockWriter

The block writer maps a placed `IExcelBlock` onto an existing ClosedXML worksheet.

```csharp
internal interface IExcelBlockWriter
{
    bool CanWrite(IExcelBlock block);

    void Write(
        IXLWorksheet worksheet,
        IExcelBlock block);
}
```

The initial `TableBlockWriter`:

- Iterates the cell matrix.
- Rejects the block if `SeedCellAddress` is `null`.
- Offsets each block-relative cell by the seed address.
- Writes either a scalar value or a formula, selecting ClosedXML's A1 or R1C1 formula property from the explicit notation.
- Translates `ExcelCellStyle` into ClosedXML style properties.
- Leaves blank cells genuinely blank unless a style requires materialising the cell.
- Adjusts populated worksheet columns to their contents after all blocks on the worksheet have been written. Explicit column-width configuration remains deferred.

ClosedXML types stay internal to the writing layer.

### ExcelDocumentWriter

```csharp
public sealed class ExcelDocumentWriter
{
    public void Write(
        ExcelDocument document,
        string filePath);
}
```

Responsibilities:

1. Validate the document and destination.
2. Create an `XLWorkbook`.
3. Create each worksheet.
4. Ask the configured strategy to place its blocks.
5. Dispatch each placed block to an appropriate block writer.
6. Apply worksheet-level options added later.
7. Save the workbook.

The writer should fail clearly when:

- The document has no worksheets.
- Worksheet names are invalid or duplicated.
- A block has no registered writer.
- A placed block exceeds worksheet limits.
- The destination directory does not exist.

The first implementation does not create missing destination directories automatically. A missing directory is treated as invalid caller input and produces a clear exception.

`TableBlock` remains the initial block name. It means a rectangular cell grid in the `AIQ.Exports` model and does not imply that the writer creates an Excel native table.

## Proposed folder structure

```text
src/AIQ.Exports/
└── Excel/
    ├── Models/
    │   ├── ExcelCell.cs
    │   ├── ExcelCellContent.cs
    │   ├── ExcelCellStyle.cs
    │   ├── ExcelCellAddress.cs
    │   ├── ExcelDocument.cs
    │   ├── ExcelWorksheetDefinition.cs
    │   ├── IExcelBlock.cs
    │   └── TableBlock.cs
    ├── Builders/
    │   └── TableBlockBuilder.cs
    ├── Placement/
    │   ├── IExcelBlockPlacementStrategy.cs
    │   └── VerticalBlockPlacementStrategy.cs
    └── Writing/
        ├── IExcelBlockWriter.cs
        ├── TableBlockWriter.cs
        └── ExcelDocumentWriter.cs
```

Names may be shortened if the namespace already makes their role obvious.

## Testing approach

Tests belong in:

```text
tests/AIQ.Exports.Tests/Excel/
```

### Builder unit tests

Focused tests should cover:

1. Rows of different lengths produce a matrix sized to the longest row.
2. A column longer than the current row count expands the block height.
3. Writing to a later column expands the width and leaves intermediate blanks.
4. Overwrite behaviour is deterministic.
5. Row styles and cell styles follow the agreed precedence rule.
6. Later structural builder mutations do not resize previously built blocks.
7. Cells within a built block can be replaced without changing its dimensions.
7. Invalid indices fail with useful exceptions.

### Placement unit tests

Focused tests should cover:

1. The first block uses the configured starting address.
2. Subsequent blocks start after the preceding block plus the configured gap.
3. Empty block lists produce no placements.
4. Negative gaps are rejected.
5. Worksheet bounds are enforced.

### Writer integration tests

Use ClosedXML to reopen the produced workbook and verify:

1. Worksheet names.
2. Block positions.
3. Scalar values and blanks.
4. Formulas.
5. Representative number, fill, and font styles.
6. Separation between vertically placed blocks.

### Inspectable sample workbook

One integration test should create a compact workbook demonstrating:

- Uneven source rows expanded into a rectangular block.
- A column modification.
- Header, body, percentage, and total styles.
- A formula cell.
- Two blocks placed vertically with a visible gap.
- Two worksheets using the same block model.

The generated file should be written beneath the test assembly output:

```text
bin/<Configuration>/net8.0/TestOutputs/AIQ.Exports.Excel.Example.xlsx
```

The test should report the absolute output path. The workbook is a review artefact rather than a committed binary.

Before the architecture phase is considered complete, the sample workbook should be opened or rendered and visually checked for:

- Correct block positions.
- Legible widths and row heights.
- Unclipped values.
- Expected number formats and colours.
- No formula errors.

## Initial implementation defaults

- Every worksheet declares its own placement strategy and starting address.
- The document writer requires the destination directory to exist.
- `TableBlock` is the canonical name for the initial rectangular cell-grid block.

## Deferred extensions

These are intentionally deferred but should fit without redesigning the initial pipeline:

- Horizontal and grid placement strategies.
- Merged cells.
- Explicit column widths and row heights beyond the initial content-based column sizing.
- Borders.
- Conditional formatting.
- Native Excel tables.
- Charts and images.
- Named ranges.
- Worksheet freeze panes and print settings.
- Loading a workbook template before writing blocks.
