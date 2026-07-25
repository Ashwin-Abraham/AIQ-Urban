using AIQ.Exports.Excel.Builders;
using AIQ.Exports.Excel.Models;
using AIQ.Exports.Excel.Placement;
using AIQ.Exports.Excel.Writing;
using ClosedXML.Excel;
using System.Drawing;

namespace AIQ.Exports.Tests.Excel;

public class ExcelDocumentWriterTests
{
    [Fact]
    public void Write_SingleWorksheetAndBlock_CreatesExpectedWorkbook()
    {
        var block = new TableBlockBuilder()
            .AddNewRow([
                ExcelCell.From("Name"),
                ExcelCell.From("Value"),
            ])
            .AddNewRow([
                ExcelCell.From("Area"),
                ExcelCell.From(1_250.0),
            ])
            .Build();
        var worksheet = new ExcelWorksheetDefinition(
            "Summary",
            [block],
            new VerticalBlockPlacementStrategy(rowGap: 1),
            new ExcelCellAddress(row: 2, column: 2));
        var document = new ExcelDocument([worksheet]);
        var outputPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.xlsx");

        try
        {
            new ExcelDocumentWriter().Write(document, outputPath);

            using var workbook = new XLWorkbook(outputPath);
            var writtenSheet = workbook.Worksheet("Summary");
            Assert.Equal("Name", writtenSheet.Cell("B2").GetString());
            Assert.Equal("Value", writtenSheet.Cell("C2").GetString());
            Assert.Equal("Area", writtenSheet.Cell("B3").GetString());
            Assert.Equal(1_250.0, writtenSheet.Cell("C3").GetDouble());
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public void Write_A1AndR1C1Formulas_PreservesNotationAndExpressions()
    {
        var block = new TableBlockBuilder()
            .AddNewRow([
                ExcelCell.FormulaA1("SUM(A2:A3)"),
                ExcelCell.FormulaR1C1("RC[-1]*2"),
            ])
            .Build();
        var document = CreateDocument(
            "Formulas",
            [block],
            new ExcelCellAddress(1, 1));
        var outputPath = TemporaryWorkbookPath();

        try
        {
            new ExcelDocumentWriter().Write(document, outputPath);

            using var workbook = new XLWorkbook(outputPath);
            var worksheet = workbook.Worksheet("Formulas");
            Assert.Equal("SUM(A2:A3)", worksheet.Cell("A1").FormulaA1);
            Assert.Equal("RC[-1]*2", worksheet.Cell("B1").FormulaR1C1);
        }
        finally
        {
            DeleteIfPresent(outputPath);
        }
    }

    [Fact]
    public void Write_StyledCells_AppliesRepresentativeClosedXmlStyles()
    {
        var style = new ExcelCellStyle(
            Bold: true,
            FontSize: 14,
            FontColor: Color.White,
            FillColor: Color.DarkBlue,
            NumberFormat: "0.0%",
            HorizontalAlignment: ExcelHorizontalAlignment.Center,
            VerticalAlignment: ExcelVerticalAlignment.Center,
            WrapText: true);
        var block = new TableBlockBuilder()
            .AddNewRow([ExcelCell.From(0.25, style)])
            .Build();
        var document = CreateDocument(
            "Styles",
            [block],
            new ExcelCellAddress(1, 1));
        var outputPath = TemporaryWorkbookPath();

        try
        {
            new ExcelDocumentWriter().Write(document, outputPath);

            using var workbook = new XLWorkbook(outputPath);
            var cell = workbook.Worksheet("Styles").Cell("A1");
            Assert.True(cell.Style.Font.Bold);
            Assert.Equal(14, cell.Style.Font.FontSize);
            Assert.Equal(
                XLColor.FromColor(Color.White),
                cell.Style.Font.FontColor);
            Assert.Equal(
                XLColor.FromColor(Color.DarkBlue),
                cell.Style.Fill.BackgroundColor);
            Assert.Equal("0.0%", cell.Style.NumberFormat.Format);
            Assert.Equal(
                XLAlignmentHorizontalValues.Center,
                cell.Style.Alignment.Horizontal);
            Assert.Equal(
                XLAlignmentVerticalValues.Center,
                cell.Style.Alignment.Vertical);
            Assert.True(cell.Style.Alignment.WrapText);
        }
        finally
        {
            DeleteIfPresent(outputPath);
        }
    }

    [Fact]
    public void Write_MultipleWorksheetsAndBlocks_UsesEachWorksheetLayout()
    {
        var firstBlock = new TableBlockBuilder()
            .AddNewRow([ExcelCell.From("First")])
            .Build();
        var secondBlock = new TableBlockBuilder()
            .AddNewRow([ExcelCell.From("Second")])
            .Build();
        var thirdBlock = new TableBlockBuilder()
            .AddNewRow([ExcelCell.From("Third")])
            .Build();
        var document = new ExcelDocument([
            new ExcelWorksheetDefinition(
                "One",
                [firstBlock, secondBlock],
                new VerticalBlockPlacementStrategy(rowGap: 2),
                new ExcelCellAddress(row: 1, column: 1)),
            new ExcelWorksheetDefinition(
                "Two",
                [thirdBlock],
                new VerticalBlockPlacementStrategy(rowGap: 1),
                new ExcelCellAddress(row: 3, column: 3)),
        ]);
        var outputPath = TemporaryWorkbookPath();

        try
        {
            new ExcelDocumentWriter().Write(document, outputPath);

            using var workbook = new XLWorkbook(outputPath);
            Assert.Equal("First", workbook.Worksheet("One").Cell("A1").GetString());
            Assert.Equal("Second", workbook.Worksheet("One").Cell("A4").GetString());
            Assert.True(workbook.Worksheet("One").Cell("A2").IsEmpty());
            Assert.True(workbook.Worksheet("One").Cell("A3").IsEmpty());
            Assert.Equal("Third", workbook.Worksheet("Two").Cell("C3").GetString());
        }
        finally
        {
            DeleteIfPresent(outputPath);
        }
    }

    private static ExcelDocument CreateDocument(
        string worksheetName,
        IReadOnlyList<IExcelBlock> blocks,
        ExcelCellAddress startingAddress)
    {
        return new ExcelDocument([
            new ExcelWorksheetDefinition(
                worksheetName,
                blocks,
                new VerticalBlockPlacementStrategy(rowGap: 1),
                startingAddress),
        ]);
    }

    private static string TemporaryWorkbookPath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.xlsx");
    }

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
