using AIQ.Exports.Excel.Builders;
using AIQ.Exports.Excel.Models;
using System.Drawing;

namespace AIQ.Exports.Tests.Excel;

public class TableBlockBuilderTests
{
    [Fact]
    public void Build_RowsHaveDifferentLengths_UsesLongestRowAndFillsBlanks()
    {
        var builder = new TableBlockBuilder()
            .AddNewRow([
                ExcelCell.From("A"),
                ExcelCell.From("B"),
                ExcelCell.From("C"),
            ])
            .AddNewRow([
                ExcelCell.From("D"),
            ]);

        var block = builder.Build();

        Assert.Equal(2, block.RowCount);
        Assert.Equal(3, block.ColumnCount);
        Assert.Equal("A", ValueAt(block, 0, 0));
        Assert.Equal("B", ValueAt(block, 0, 1));
        Assert.Equal("C", ValueAt(block, 0, 2));
        Assert.Equal("D", ValueAt(block, 1, 0));
        Assert.IsType<ExcelCellContent.Blank>(block.Cells[1, 1].Content);
        Assert.IsType<ExcelCellContent.Blank>(block.Cells[1, 2].Content);
    }

    [Fact]
    public void Build_BuilderChangesAfterFirstBuild_DoNotResizePreviousBlock()
    {
        var builder = new TableBlockBuilder()
            .AddNewRow([
                ExcelCell.From("A"),
                ExcelCell.From("B"),
            ]);

        var first = builder.Build();
        builder.AddNewRow([
            ExcelCell.From("C"),
            ExcelCell.From("D"),
            ExcelCell.From("E"),
        ]);
        var second = builder.Build();

        Assert.Equal(1, first.RowCount);
        Assert.Equal(2, first.ColumnCount);
        Assert.Equal(2, second.RowCount);
        Assert.Equal(3, second.ColumnCount);
    }

    [Fact]
    public void ModifyColumn_DataLongerThanRows_ExpandsNextBuiltBlock()
    {
        var builder = new TableBlockBuilder()
            .AddNewRow([
                ExcelCell.From("Heading"),
            ])
            .ModifyColumn(
                columnIndex: 1,
                data: [
                    ExcelCell.From(10),
                    ExcelCell.From(20),
                    ExcelCell.From(30),
                ],
                overwriteExistingData: true);

        var block = builder.Build();

        Assert.Equal(3, block.RowCount);
        Assert.Equal(2, block.ColumnCount);
        Assert.Equal(10, ValueAt(block, 0, 1));
        Assert.Equal(20, ValueAt(block, 1, 1));
        Assert.Equal(30, ValueAt(block, 2, 1));
    }

    [Fact]
    public void ModifyColumn_LaterColumn_LeavesIntermediateCellsBlank()
    {
        var block = new TableBlockBuilder()
            .ModifyColumn(
                columnIndex: 2,
                data: [ExcelCell.From("C")],
                overwriteExistingData: true)
            .Build();

        Assert.Equal(3, block.ColumnCount);
        Assert.IsType<ExcelCellContent.Blank>(block.Cells[0, 0].Content);
        Assert.IsType<ExcelCellContent.Blank>(block.Cells[0, 1].Content);
        Assert.Equal("C", ValueAt(block, 0, 2));
    }

    [Fact]
    public void ModifyColumn_OverwriteDisabled_PreservesPopulatedCells()
    {
        var block = new TableBlockBuilder()
            .AddNewRow([ExcelCell.From("Existing")])
            .ModifyColumn(
                columnIndex: 0,
                data: [ExcelCell.From("Incoming")],
                overwriteExistingData: false)
            .Build();

        Assert.Equal("Existing", ValueAt(block, 0, 0));
    }

    [Fact]
    public void ModifyColumn_OverwriteDisabled_WritesStyledBlankCells()
    {
        var blankStyle = new ExcelCellStyle(FillColor: Color.Orange);
        var operationStyle = new ExcelCellStyle(NumberFormat: "0.0%");
        var block = new TableBlockBuilder()
            .AddNewRow([ExcelCell.Blank(blankStyle)])
            .ModifyColumn(
                columnIndex: 0,
                data: [ExcelCell.From(0.25)],
                overwriteExistingData: false,
                style: operationStyle)
            .Build();

        Assert.Equal(0.25, ValueAt(block, 0, 0));
        Assert.Equal(Color.Orange, block.Cells[0, 0].Style?.FillColor);
        Assert.Equal("0.0%", block.Cells[0, 0].Style?.NumberFormat);
    }

    [Fact]
    public void ModifyColumn_OverwriteEnabled_ReplacesContentAndMergesStyle()
    {
        var block = new TableBlockBuilder()
            .AddNewRow([
                ExcelCell.From(
                    "Existing",
                    new ExcelCellStyle(Bold: true)),
            ])
            .ModifyColumn(
                columnIndex: 0,
                data: [
                    ExcelCell.From(
                        "Incoming",
                        new ExcelCellStyle(FontSize: 12)),
                ],
                overwriteExistingData: true,
                style: new ExcelCellStyle(FillColor: Color.LightBlue))
            .Build();

        Assert.Equal("Incoming", ValueAt(block, 0, 0));
        var style = Assert.IsType<ExcelCellStyle>(block.Cells[0, 0].Style);
        Assert.True(style.Bold);
        Assert.Equal(12, style.FontSize);
        Assert.Equal(Color.LightBlue, style.FillColor);
    }

    [Fact]
    public void ModifyColumn_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        var builder = new TableBlockBuilder();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.ModifyColumn(
                columnIndex: -1,
                data: [ExcelCell.From("Value")],
                overwriteExistingData: true));
    }

    [Fact]
    public void AddNewRow_RowAndCellStyles_CellPropertiesWin()
    {
        var rowStyle = new ExcelCellStyle(
            Bold: true,
            FillColor: Color.Orange,
            NumberFormat: "#,##0");
        var cellStyle = new ExcelCellStyle(NumberFormat: "0.0%");

        var block = new TableBlockBuilder()
            .AddNewRow(
                [ExcelCell.From(0.25, cellStyle)],
                rowStyle)
            .Build();

        var style = Assert.IsType<ExcelCellStyle>(block.Cells[0, 0].Style);
        Assert.True(style.Bold);
        Assert.Equal(Color.Orange, style.FillColor);
        Assert.Equal("0.0%", style.NumberFormat);
    }

    [Fact]
    public void SetRowStyle_PartialStyle_PreservesUnspecifiedProperties()
    {
        var block = new TableBlockBuilder()
            .AddNewRow([
                ExcelCell.From(
                    "Heading",
                    new ExcelCellStyle(Bold: true, FillColor: Color.Orange)),
            ])
            .SetRowStyle(
                rowIndex: 0,
                new ExcelCellStyle(HorizontalAlignment: ExcelHorizontalAlignment.Center))
            .Build();

        var style = Assert.IsType<ExcelCellStyle>(block.Cells[0, 0].Style);
        Assert.True(style.Bold);
        Assert.Equal(Color.Orange, style.FillColor);
        Assert.Equal(ExcelHorizontalAlignment.Center, style.HorizontalAlignment);
    }

    [Fact]
    public void SetRowStyle_RowDoesNotExist_ThrowsArgumentOutOfRangeException()
    {
        var builder = new TableBlockBuilder();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.SetRowStyle(0, new ExcelCellStyle(Bold: true)));
    }

    [Fact]
    public void Cells_ReplaceCellAfterBuild_DimensionsRemainFixed()
    {
        var block = new TableBlockBuilder()
            .AddNewRow([ExcelCell.From("Before")])
            .Build();

        block.Cells[0, 0] = ExcelCell.From("After");

        Assert.Equal(1, block.RowCount);
        Assert.Equal(1, block.ColumnCount);
        Assert.Equal("After", ValueAt(block, 0, 0));
    }

    private static object ValueAt(
        TableBlock block,
        int rowIndex,
        int columnIndex)
    {
        return Assert.IsType<ExcelCellContent.Value>(
            block.Cells[rowIndex, columnIndex].Content).Data;
    }
}
