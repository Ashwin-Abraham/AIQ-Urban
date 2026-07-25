using System.Drawing;
using AIQ.Exports.Excel.Builders;
using AIQ.Exports.Excel.Models;
using AIQ.Exports.Excel.Placement;

namespace AIQ.Exports.Excel.Reports;

public static class MetricsWorkbookBuilder
{
    private const int ReportColumnCount = 7;

    private static readonly ExcelCellStyle _reportTitleStyle = new(
        Bold: true,
        FontSize: 16,
        FontColor: Color.White,
        FillColor: Color.FromArgb(31, 78, 121),
        HorizontalAlignment: ExcelHorizontalAlignment.Left);

    private static readonly ExcelCellStyle _sectionTitleStyle = new(
        Bold: true,
        FontSize: 13,
        FontColor: Color.FromArgb(31, 78, 121),
        FillColor: Color.FromArgb(217, 234, 247),
        HorizontalAlignment: ExcelHorizontalAlignment.Left);

    private static readonly ExcelCellStyle _headerStyle = new(
        Bold: true,
        FontColor: Color.White,
        FillColor: Color.FromArgb(68, 114, 196),
        HorizontalAlignment: ExcelHorizontalAlignment.Center,
        VerticalAlignment: ExcelVerticalAlignment.Center,
        WrapText: true);

    private static readonly ExcelCellStyle _subtotalStyle = new(
        Bold: true,
        FillColor: Color.FromArgb(221, 235, 247));

    private static readonly ExcelCellStyle _totalStyle = new(
        Bold: true,
        FontColor: Color.White,
        FillColor: Color.FromArgb(31, 78, 121));

    private static readonly ExcelCellStyle _numberStyle = new(
        NumberFormat: "#,##0.00");

    private static readonly ExcelCellStyle _integerStyle = new(
        NumberFormat: "#,##0");

    private static readonly ExcelCellStyle _percentageStyle = new(
        NumberFormat: "0.0%");

    private static readonly ExcelCellStyle _ratioStyle = new(
        NumberFormat: "0.00");

    public static ExcelDocument Build(MetricsWorkbookData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Worksheets.Count == 0)
        {
            throw new ArgumentException(
                "A metrics workbook must contain at least one worksheet.",
                nameof(data));
        }

        var worksheetNames = ResolveWorksheetNames(
            data.Worksheets.Select(worksheet => worksheet.Name));

        var worksheets = data.Worksheets
            .Zip(worksheetNames, BuildWorksheet)
            .ToList();

        return new ExcelDocument(worksheets);
    }

    private static ExcelWorksheetDefinition BuildWorksheet(
        MetricsWorksheetData data,
        string worksheetName)
    {
        var composer = new WorksheetComposer();
        composer.AddBand($"PROJECT METRICS - {data.Name}", _reportTitleStyle);
        composer.AddBlankRow();

        var landAreaCell = AddOverallMetrics(composer, data);
        composer.AddBlankRow();
        AddUseAreaSection(
            composer,
            "LAND AREA BY USE",
            "Land Area",
            data.LandAreaByUse,
            landAreaCell,
            includeFarContribution: false);
        composer.AddBlankRow();
        AddUseAreaSection(
            composer,
            "BUILT AREA BY USE",
            "Built Area",
            data.BuiltAreaByUse,
            landAreaCell,
            includeFarContribution: true);
        composer.AddBlankRow();
        AddTransitSection(composer, data.TransitMetrics);

        return new ExcelWorksheetDefinition(
            worksheetName,
            [composer.Build()],
            new VerticalBlockPlacementStrategy(rowGap: 0),
            new ExcelCellAddress(row: 1, column: 1),
            new ExcelWorksheetOptions(
                HideGridlines: true,
                FrozenRowCount: 1,
                ColumnWidths: new Dictionary<int, double>
                {
                    [1] = 24,
                    [2] = 13,
                    [3] = 16,
                    [4] = 18,
                    [5] = 18,
                    [6] = 16,
                    [7] = 16,
                }));
    }

    private static string AddOverallMetrics(
        WorksheetComposer composer,
        MetricsWorksheetData data)
    {
        composer.AddBand("OVERALL ZONE METRICS", _sectionTitleStyle);
        composer.AddRow(
            [
                ExcelCell.From("Metric"),
                ExcelCell.From("Value"),
                ExcelCell.From("Unit"),
            ],
            _headerStyle);

        var landAreaRow = composer.NextRow;
        composer.AddMetric("Total Land Area", data.TotalLandArea, "model units²", _numberStyle);
        composer.AddMetric("Total Footprint Area", data.TotalFootprintArea, "model units²", _numberStyle);
        composer.AddMetric("Total Built Area", data.TotalBuiltArea, "model units²", _numberStyle);
        composer.AddMetric("Total Transit Length", data.TotalTransitLength, "model units", _numberStyle);
        composer.AddMetric("Total Transit Area", data.TotalTransitArea, "model units²", _numberStyle);
        composer.AddMetric("Building Count", data.BuildingCount, "count", _integerStyle);
        composer.AddMetric("Parcel Count", data.ParcelCount, "count", _integerStyle);
        composer.AddMetric("Transit Count", data.TransitCount, "count", _integerStyle);
        composer.AddMetric("Coverage", data.CoverageRatio, "ratio", _percentageStyle);
        composer.AddMetric("FAR", data.FAR, "ratio", _ratioStyle);

        return $"$B${landAreaRow}";
    }

    private static void AddUseAreaSection(
        WorksheetComposer composer,
        string sectionName,
        string areaColumnName,
        IReadOnlyList<UseAreaData> sourceRows,
        string landAreaCell,
        bool includeFarContribution)
    {
        composer.AddBand(sectionName, _sectionTitleStyle);

        var headers = new List<ExcelCell>
        {
            ExcelCell.From("Use"),
            ExcelCell.From("Colour"),
            ExcelCell.From(areaColumnName),
            ExcelCell.From("Share"),
        };
        if (includeFarContribution)
            headers.Add(ExcelCell.From("FAR Contribution"));
        composer.AddRow(headers, _headerStyle);

        var sortedRows = sourceRows
            .OrderBy(row => row.UseName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var firstDataRow = composer.NextRow;
        var totalRow = firstDataRow + sortedRows.Count;

        foreach (var row in sortedRows)
        {
            var currentRow = composer.NextRow;
            var cells = new List<ExcelCell>
            {
                ExcelCell.From(row.UseName),
                ExcelCell.Blank(new ExcelCellStyle(FillColor: row.Color)),
                ExcelCell.From(row.Area, _numberStyle),
                ExcelCell.FormulaA1(
                    $"IFERROR(C{currentRow}/C{totalRow},0)",
                    _percentageStyle),
            };
            if (includeFarContribution)
            {
                cells.Add(ExcelCell.FormulaA1(
                    $"IFERROR(C{currentRow}/{landAreaCell},0)",
                    _ratioStyle));
            }
            composer.AddRow(cells);
        }

        var totalCells = new List<ExcelCell>
        {
            ExcelCell.From("Total"),
            ExcelCell.Blank(),
            sortedRows.Count == 0
                ? ExcelCell.From(0.0, _numberStyle)
                : ExcelCell.FormulaA1(
                    $"SUM(C{firstDataRow}:C{totalRow - 1})",
                    _numberStyle),
            sortedRows.Count == 0
                ? ExcelCell.From(0.0, _percentageStyle)
                : ExcelCell.FormulaA1(
                    $"SUM(D{firstDataRow}:D{totalRow - 1})",
                    _percentageStyle),
        };
        if (includeFarContribution)
        {
            totalCells.Add(sortedRows.Count == 0
                ? ExcelCell.From(0.0, _ratioStyle)
                : ExcelCell.FormulaA1(
                    $"IFERROR(C{totalRow}/{landAreaCell},0)",
                    _ratioStyle));
        }
        composer.AddRow(totalCells, _totalStyle);
    }

    private static void AddTransitSection(
        WorksheetComposer composer,
        IReadOnlyList<TransitMetricData> sourceRows)
    {
        composer.AddBand("TRANSIT BY TYPE AND WIDTH", _sectionTitleStyle);
        composer.AddRow(
            [
                ExcelCell.From("Transit Type"),
                ExcelCell.From("Width"),
                ExcelCell.From("Corridors"),
                ExcelCell.From("Running Length"),
                ExcelCell.From("Length Share"),
                ExcelCell.From("Area"),
                ExcelCell.From("Area Share"),
            ],
            _headerStyle);

        var groups = sourceRows
            .GroupBy(row => row.Type)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Type = group.Key,
                Widths = group
                    .GroupBy(row => row.Width)
                    .OrderBy(widthGroup => widthGroup.Key)
                    .Select(widthGroup => new
                    {
                        Width = widthGroup.Key,
                        Count = widthGroup.Count(),
                        RunningLength = widthGroup.Sum(row => row.RunningLength),
                        Area = widthGroup.Sum(row => row.Area),
                    })
                    .ToList(),
            })
            .ToList();

        var grandTotalRow = composer.NextRow
            + groups.Sum(group => group.Widths.Count + 1);
        var typeTotalRows = new List<int>();

        foreach (var group in groups)
        {
            var firstDetailRow = composer.NextRow;

            foreach (var width in group.Widths)
            {
                var currentRow = composer.NextRow;
                composer.AddRow([
                    ExcelCell.From(group.Type),
                    ExcelCell.From(width.Width, _numberStyle),
                    ExcelCell.From(width.Count, _integerStyle),
                    ExcelCell.From(width.RunningLength, _numberStyle),
                    ExcelCell.FormulaA1(
                        $"IFERROR(D{currentRow}/D{grandTotalRow},0)",
                        _percentageStyle),
                    ExcelCell.From(width.Area, _numberStyle),
                    ExcelCell.FormulaA1(
                        $"IFERROR(F{currentRow}/F{grandTotalRow},0)",
                        _percentageStyle),
                ]);
            }

            var typeTotalRow = composer.NextRow;
            typeTotalRows.Add(typeTotalRow);
            composer.AddRow(
                [
                    ExcelCell.From($"{group.Type} Total"),
                    ExcelCell.Blank(),
                    ExcelCell.FormulaA1(
                        $"SUM(C{firstDetailRow}:C{typeTotalRow - 1})",
                        _integerStyle),
                    ExcelCell.FormulaA1(
                        $"SUM(D{firstDetailRow}:D{typeTotalRow - 1})",
                        _numberStyle),
                    ExcelCell.FormulaA1(
                        $"IFERROR(D{typeTotalRow}/D{grandTotalRow},0)",
                        _percentageStyle),
                    ExcelCell.FormulaA1(
                        $"SUM(F{firstDetailRow}:F{typeTotalRow - 1})",
                        _numberStyle),
                    ExcelCell.FormulaA1(
                        $"IFERROR(F{typeTotalRow}/F{grandTotalRow},0)",
                        _percentageStyle),
                ],
                _subtotalStyle);
        }

        composer.AddRow(
            [
                ExcelCell.From("Grand Total"),
                ExcelCell.Blank(),
                BuildSumOfCells("C", typeTotalRows, _integerStyle),
                BuildSumOfCells("D", typeTotalRows, _numberStyle),
                typeTotalRows.Count == 0
                    ? ExcelCell.From(0.0, _percentageStyle)
                    : ExcelCell.From(1.0, _percentageStyle),
                BuildSumOfCells("F", typeTotalRows, _numberStyle),
                typeTotalRows.Count == 0
                    ? ExcelCell.From(0.0, _percentageStyle)
                    : ExcelCell.From(1.0, _percentageStyle),
            ],
            _totalStyle);
    }

    private static ExcelCell BuildSumOfCells(
        string column,
        IReadOnlyList<int> rows,
        ExcelCellStyle style)
    {
        if (rows.Count == 0)
            return ExcelCell.From(0.0, style);

        var references = string.Join(",", rows.Select(row => $"{column}{row}"));
        return ExcelCell.FormulaA1($"SUM({references})", style);
    }

    private static List<string> ResolveWorksheetNames(
        IEnumerable<string> names)
    {
        var resolvedNames = new List<string>();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var requestedName in names)
        {
            var baseName = SanitizeWorksheetName(requestedName);
            var candidate = baseName;
            var suffixNumber = 2;

            while (!usedNames.Add(candidate))
            {
                var suffix = $" ({suffixNumber})";
                var prefixLength = Math.Min(baseName.Length, 31 - suffix.Length);
                candidate = $"{baseName[..prefixLength]}{suffix}";
                suffixNumber++;
            }

            resolvedNames.Add(candidate);
        }

        return resolvedNames;
    }

    private static string SanitizeWorksheetName(string name)
    {
        var sanitized = string.IsNullOrWhiteSpace(name)
            ? "Zone"
            : string.Concat(name.Trim().Select(character =>
                character is '[' or ']' or ':' or '*' or '?' or '/' or '\\'
                    ? '-'
                    : character));

        sanitized = sanitized.Trim('\'');
        if (sanitized.Length == 0)
            sanitized = "Zone";

        return sanitized[..Math.Min(sanitized.Length, 31)];
    }

    private sealed class WorksheetComposer
    {
        private readonly TableBlockBuilder _builder = new();

        public int NextRow { get; private set; } = 1;

        public void AddBand(
            string text,
            ExcelCellStyle style)
        {
            var cells = new List<ExcelCell> { ExcelCell.From(text) };
            while (cells.Count < ReportColumnCount)
                cells.Add(ExcelCell.Blank());

            AddRow(cells, style);
        }

        public void AddMetric(
            string name,
            object value,
            string unit,
            ExcelCellStyle valueStyle)
        {
            AddRow([
                ExcelCell.From(name),
                ExcelCell.From(value, valueStyle),
                ExcelCell.From(unit),
            ]);
        }

        public void AddBlankRow()
        {
            AddRow([ExcelCell.Blank()]);
        }

        public void AddRow(
            IEnumerable<ExcelCell> cells,
            ExcelCellStyle? style = null)
        {
            _builder.AddNewRow(cells, style);
            NextRow++;
        }

        public TableBlock Build()
        {
            return _builder.Build();
        }
    }
}
