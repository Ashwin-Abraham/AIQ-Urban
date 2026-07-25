using System.Drawing;
using AIQ.Exports.Excel.Builders;
using AIQ.Exports.Excel.Models;
using AIQ.Exports.Excel.Placement;
using AIQ.Exports.Excel.Writing;
using ClosedXML.Excel;
using Xunit.Abstractions;

namespace AIQ.Exports.Tests.Excel;

public class ExcelExampleWorkbookTests(ITestOutputHelper output)
{
    [Fact]
    public void Write_ExampleWorkbook_CreatesInspectableOutput()
    {
        var document = BuildExampleDocument();
        var outputDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "TestOutputs");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(
            outputDirectory,
            "AIQ.Exports.Excel.Example.xlsx");

        if (File.Exists(outputPath))
            File.Delete(outputPath);

        new ExcelDocumentWriter().Write(document, outputPath);

        using var workbook = new XLWorkbook(outputPath);
        Assert.Equal(2, workbook.Worksheets.Count);
        Assert.Equal(
            "PROJECT METRICS",
            workbook.Worksheet("Vertical Layout").Cell("A1").GetString());
        Assert.Equal(
            "USE BREAKDOWN",
            workbook.Worksheet("Vertical Layout").Cell("A8").GetString());
        Assert.Equal(
            "SUM(B3:B4)",
            workbook.Worksheet("Vertical Layout").Cell("B5").FormulaA1);
        Assert.Equal(
            "SECOND WORKSHEET",
            workbook.Worksheet("Offset Layout").Cell("C3").GetString());
        Assert.True(File.Exists(outputPath));

        output.WriteLine($"Inspectable workbook: {outputPath}");
    }

    private static ExcelDocument BuildExampleDocument()
    {
        var titleStyle = new ExcelCellStyle(
            Bold: true,
            FontSize: 14,
            FontColor: Color.FromArgb(31, 78, 121),
            FillColor: Color.FromArgb(217, 234, 247),
            HorizontalAlignment: ExcelHorizontalAlignment.Left);
        var headerStyle = new ExcelCellStyle(
            Bold: true,
            FontColor: Color.FromArgb(31, 78, 121),
            FillColor: Color.FromArgb(217, 225, 242),
            HorizontalAlignment: ExcelHorizontalAlignment.Center,
            WrapText: true);
        var numberStyle = new ExcelCellStyle(NumberFormat: "#,##0.00");
        var percentageStyle = new ExcelCellStyle(NumberFormat: "0.0%");
        var totalStyle = new ExcelCellStyle(
            Bold: true,
            FillColor: Color.FromArgb(217, 225, 242));

        var metrics = new TableBlockBuilder()
            .AddNewRow([ExcelCell.From("PROJECT METRICS")], titleStyle)
            .AddNewRow([
                ExcelCell.From("Metric"),
                ExcelCell.From("Value"),
            ], headerStyle)
            .AddNewRow([
                ExcelCell.From("Land Area"),
                ExcelCell.From(12_500.0, numberStyle),
            ])
            .AddNewRow([
                ExcelCell.From("Built Area"),
                ExcelCell.From(4_750.0, numberStyle),
            ])
            .AddNewRow([
                ExcelCell.From("Total"),
                ExcelCell.FormulaA1("SUM(B3:B4)", numberStyle),
            ], totalStyle)
            .ModifyColumn(
                columnIndex: 2,
                data: [
                    ExcelCell.Blank(),
                    ExcelCell.From("Share", headerStyle),
                    ExcelCell.FormulaR1C1("RC[-1]/R5C2"),
                    ExcelCell.FormulaR1C1("RC[-1]/R5C2"),
                    ExcelCell.FormulaA1("SUM(C3:C4)"),
                ],
                overwriteExistingData: true,
                style: percentageStyle)
            .Build();

        var uses = new TableBlockBuilder()
            .AddNewRow([ExcelCell.From("USE BREAKDOWN")], titleStyle)
            .AddNewRow([
                ExcelCell.From("Use"),
                ExcelCell.From("Area"),
                ExcelCell.From("Share"),
            ], headerStyle)
            .AddNewRow([
                ExcelCell.From("Residential"),
                ExcelCell.From(7_500.0, numberStyle),
                ExcelCell.From(0.60, percentageStyle),
            ])
            .AddNewRow([
                ExcelCell.From("Commercial"),
                ExcelCell.From(3_125.0, numberStyle),
                ExcelCell.From(0.25, percentageStyle),
            ])
            .AddNewRow([
                ExcelCell.From("Public"),
                ExcelCell.From(1_875.0, numberStyle),
                ExcelCell.From(0.15, percentageStyle),
            ])
            .Build();

        var offsetBlock = new TableBlockBuilder()
            .AddNewRow([ExcelCell.From("SECOND WORKSHEET")], titleStyle)
            .AddNewRow([
                ExcelCell.From("This block begins at C3."),
                ExcelCell.From("The layout belongs to its worksheet."),
            ])
            .Build();

        return new ExcelDocument([
            new ExcelWorksheetDefinition(
                "Vertical Layout",
                [metrics, uses],
                new VerticalBlockPlacementStrategy(rowGap: 2),
                new ExcelCellAddress(row: 1, column: 1)),
            new ExcelWorksheetDefinition(
                "Offset Layout",
                [offsetBlock],
                new VerticalBlockPlacementStrategy(rowGap: 1),
                new ExcelCellAddress(row: 3, column: 3)),
        ]);
    }
}
