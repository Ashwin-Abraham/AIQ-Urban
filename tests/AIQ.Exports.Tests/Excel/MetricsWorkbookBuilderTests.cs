using System.Drawing;
using AIQ.Exports.Excel.Reports;
using AIQ.Exports.Excel.Writing;
using ClosedXML.Excel;

namespace AIQ.Exports.Tests.Excel;

public class MetricsWorkbookBuilderTests
{
    [Fact]
    public void Build_RepresentativeReport_CreatesAcceptedProjectAndZoneTables()
    {
        var outputPath = WriteWorkbook(CreateWorkbookData());

        try
        {
            using var workbook = new XLWorkbook(outputPath);

            Assert.Equal(
                [
                    "Project Overview",
                    "Zone - North-Zone",
                    "Zone - North-Zone (2)",
                    "Zone - Empty",
                ],
                workbook.Worksheets.Select(sheet => sheet.Name));

            var project = workbook.Worksheet("Project Overview");
            Assert.Equal("Sample Project", project.Cell("A1").GetString());
            Assert.Equal(
                "Exported: 2026-08-04 14:30",
                project.Cell("A2").GetString());
            Assert.NotNull(FindCell(project, "PROJECT SUMMARY"));
            Assert.NotNull(FindCell(project, "PARCEL USE SUMMARY"));
            Assert.NotNull(FindCell(project, "BUILT-PLATE USE SUMMARY"));
            Assert.NotNull(FindCell(project, "TRANSIT SUMMARY"));
            Assert.NotNull(FindCell(project, "ZONE COMPARISON"));
            Assert.DoesNotContain(
                project.CellsUsed(),
                cell => cell.GetString() == "FAR by Use");
            Assert.DoesNotContain(
                project.CellsUsed(),
                cell => cell.GetString() == "Floor Plate Count");

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

    [Fact]
    public void Build_DerivedCells_UsesGuardedFormulasAndAcceptedFormats()
    {
        var outputPath = WriteWorkbook(CreateWorkbookData());

        try
        {
            using var workbook = new XLWorkbook(outputPath);
            var project = workbook.Worksheet("Project Overview");
            var coverage = FindSummaryMetric(project, "Coverage").Cell(2);
            var far = FindSummaryMetric(project, "FAR").Cell(2);

            Assert.Contains("IF(", coverage.FormulaA1);
            Assert.Contains("=0,0", coverage.FormulaA1);
            Assert.Equal("0.0%", coverage.Style.NumberFormat.Format);
            Assert.Contains("IF(", far.FormulaA1);
            Assert.Equal("0.00", far.Style.NumberFormat.Format);

            var residentialParcel = project.RowsUsed()
                .Single(row =>
                    row.Cell(1).GetString() == "Residential"
                    && row.Cell(3).GetDouble() == 2);
            Assert.Equal(
                "#,##0.00",
                residentialParcel.Cell(4).Style.NumberFormat.Format);
            Assert.Equal(
                "0.0%",
                residentialParcel.Cell(5).Style.NumberFormat.Format);
        }
        finally
        {
            DeleteIfPresent(outputPath);
        }
    }

    [Fact]
    public void Build_EmptyZone_PreservesEachExpectedTableWithNoDataRow()
    {
        var outputPath = WriteWorkbook(CreateWorkbookData());

        try
        {
            using var workbook = new XLWorkbook(outputPath);
            var zone = workbook.Worksheet("Zone - Empty");
            Assert.Equal(
                3,
                zone.CellsUsed().Count(cell => cell.GetString() == "No data"));
            Assert.NotNull(FindCell(zone, "PARCEL USE SUMMARY"));
            Assert.NotNull(FindCell(zone, "BUILT-PLATE USE SUMMARY"));
            Assert.NotNull(FindCell(zone, "TRANSIT SUMMARY"));
        }
        finally
        {
            DeleteIfPresent(outputPath);
        }
    }

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
                    "Zone",
                    "Zone Boundary Area",
                    "Assigned Parcel Area",
                    "Building Footprint",
                    "Coverage",
                    "Built Area",
                    "FAR",
                    "Transit Length",
                    "Transit Area",
                ],
                headerRow.Cells(1, 9).Select(cell => cell.GetString()));

            var lastRow = project.LastRowUsed()!.RowNumber();
            Assert.DoesNotContain(
                project.Rows(heading.Address.RowNumber + 2, lastRow),
                row => row.Cell(1).GetString() is "Total" or "Grand Total");
        }
        finally
        {
            DeleteIfPresent(outputPath);
        }
    }

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
                    "Residential",
                    Color.Gold,
                    2,
                    1_000,
                    140,
                    200),
            ],
            BuiltPlateUses:
            [
                new BuiltPlateUseData("Residential", Color.Gold, 600),
            ],
            TransitMetrics:
            [
                new TransitMetricData(
                    "Road", 6, 10, 60, Color.FromArgb(90, 100, 110)),
                new TransitMetricData(
                    "Road", 6, 20, 120, Color.FromArgb(90, 100, 110)),
                new TransitMetricData(
                    "Road", 10, 5, 50, Color.FromArgb(70, 80, 90)),
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

    private static IXLCell FindCell(
        IXLWorksheet worksheet,
        string value)
    {
        return worksheet.CellsUsed()
            .Single(cell => cell.GetString() == value);
    }

    private static IXLCell? FindOptionalCell(
        IXLWorksheet worksheet,
        string value)
    {
        return worksheet.CellsUsed()
            .FirstOrDefault(cell => cell.GetString() == value);
    }

    private static IXLRow FindSummaryMetric(
        IXLWorksheet worksheet,
        string metric)
    {
        var summaryHeading = FindOptionalCell(worksheet, "PROJECT SUMMARY")
            ?? FindCell(worksheet, "ZONE SUMMARY");
        var firstMetricRow = summaryHeading.Address.RowNumber + 2;
        return worksheet.Rows(firstMetricRow, firstMetricRow + 10)
            .Single(row => row.Cell(1).GetString() == metric);
    }

    private static string TemporaryWorkbookPath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}-metrics.xlsx");
    }

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
