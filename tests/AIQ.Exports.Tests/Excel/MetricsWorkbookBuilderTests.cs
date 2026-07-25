using System.Drawing;
using AIQ.Exports.Excel.Reports;
using AIQ.Exports.Excel.Writing;
using ClosedXML.Excel;

namespace AIQ.Exports.Tests.Excel;

public class MetricsWorkbookBuilderTests
{
    [Fact]
    public void Build_ZoneMetrics_CreatesGroupedAndFormulaDrivenWorkbook()
    {
        // Arrange
        var data = new MetricsWorkbookData([
            CreateWorksheet("North/Zone"),
            CreateWorksheet("north-zone"),
        ]);
        var outputPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}-metrics.xlsx");

        try
        {
            // Act
            var document = MetricsWorkbookBuilder.Build(data);
            new ExcelDocumentWriter().Write(document, outputPath);

            // Assert
            using var workbook = new XLWorkbook(outputPath);
            Assert.Equal(
                ["North-Zone", "north-zone (2)"],
                workbook.Worksheets.Select(sheet => sheet.Name));

            var worksheet = workbook.Worksheet("North-Zone");
            Assert.False(worksheet.ShowGridLines);
            Assert.Equal(
                "PROJECT METRICS - North/Zone",
                worksheet.Cell("A1").GetString());
            Assert.NotNull(FindCell(worksheet, "OVERALL ZONE METRICS"));
            Assert.NotNull(FindCell(worksheet, "LAND AREA BY USE"));
            Assert.NotNull(FindCell(worksheet, "BUILT AREA BY USE"));
            Assert.NotNull(FindCell(worksheet, "TRANSIT BY TYPE AND WIDTH"));

            var residentialLand = worksheet.CellsUsed()
                .First(cell => cell.GetString() == "Residential");
            Assert.Equal(
                $"IFERROR(C{residentialLand.Address.RowNumber}/C"
                + $"{residentialLand.Address.RowNumber + 1},0)",
                residentialLand.CellRight(3).FormulaA1);

            var roadWidthSix = worksheet.CellsUsed()
                .Single(cell =>
                    cell.GetString() == "Road"
                    && cell.CellRight().GetDouble() == 6);
            Assert.Equal(2, roadWidthSix.CellRight(2).GetDouble());
            Assert.Equal(30, roadWidthSix.CellRight(3).GetDouble());

            var roadTotal = FindCell(worksheet, "Road Total");
            Assert.StartsWith("SUM(C", roadTotal.CellRight(2).FormulaA1);
            Assert.Equal(3, roadTotal.CellRight(2).GetDouble());
            Assert.Equal(35, roadTotal.CellRight(3).GetDouble());
            Assert.Equal(
                "Grand Total",
                worksheet.CellsUsed()
                    .Single(cell => cell.GetString() == "Grand Total")
                    .GetString());
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    private static MetricsWorksheetData CreateWorksheet(string name)
    {
        return new MetricsWorksheetData(
            Name: name,
            TotalLandArea: 1000,
            TotalFootprintArea: 200,
            TotalBuiltArea: 600,
            TotalTransitLength: 50,
            TotalTransitArea: 300,
            BuildingCount: 3,
            ParcelCount: 2,
            TransitCount: 4,
            CoverageRatio: 0.2,
            FAR: 0.6,
            LandAreaByUse: [
                new UseAreaData("Residential", Color.Gold, 700),
                new UseAreaData("Commercial", Color.CornflowerBlue, 300),
            ],
            BuiltAreaByUse: [
                new UseAreaData("Residential", Color.Gold, 400),
                new UseAreaData("Commercial", Color.CornflowerBlue, 200),
            ],
            TransitMetrics: [
                new TransitMetricData("Road", 6, 10, 60),
                new TransitMetricData("Road", 6, 20, 120),
                new TransitMetricData("Road", 10, 5, 50),
                new TransitMetricData("Railway", 4, 15, 60),
            ]);
    }

    private static IXLCell FindCell(
        IXLWorksheet worksheet,
        string value)
    {
        return worksheet.CellsUsed()
            .Single(cell => cell.GetString() == value);
    }
}
