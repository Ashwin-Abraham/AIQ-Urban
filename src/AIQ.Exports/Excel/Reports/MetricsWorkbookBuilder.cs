using System.Drawing;
using AIQ.Exports.Excel.Builders;
using AIQ.Exports.Excel.Models;
using AIQ.Exports.Excel.Placement;

namespace AIQ.Exports.Excel.Reports;

public static class MetricsWorkbookBuilder
{
    private const int ReportColumnCount = 9;
    private const string ProjectWorksheetName = "Project Overview";

    private static readonly Color _reportBlue = Color.FromArgb(51, 60, 85);

    private static readonly ExcelCellStyle _reportTitleStyle = new(
        Bold: true,
        FontSize: 16,
        FontColor: Color.White,
        FillColor: _reportBlue,
        HorizontalAlignment: ExcelHorizontalAlignment.Left);

    private static readonly ExcelCellStyle _sectionTitleStyle = new(
        Bold: true,
        FontSize: 13,
        FontColor: Color.White,
        FillColor: _reportBlue,
        HorizontalAlignment: ExcelHorizontalAlignment.Left);

    private static readonly ExcelCellStyle _headerStyle = new(
        Bold: true,
        FontColor: Color.White,
        FillColor: _reportBlue,
        HorizontalAlignment: ExcelHorizontalAlignment.Center,
        VerticalAlignment: ExcelVerticalAlignment.Center,
        WrapText: true);

    private static readonly ExcelCellStyle _subtotalStyle = new(
        Bold: true,
        FillColor: Color.FromArgb(221, 235, 247));

    private static readonly ExcelCellStyle _totalStyle = new(
        Bold: true,
        FontColor: Color.White,
        FillColor: _reportBlue);

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

        var worksheets = new List<ExcelWorksheetDefinition>
        {
            BuildWorksheet(
                data.ProjectName,
                data.ExportedAt,
                data.Project,
                ProjectWorksheetName,
                data.Zones),
        };

        var zoneNames = ResolveZoneWorksheetNames(
            data.Zones.Select(zone => zone.DisplayName));
        worksheets.AddRange(data.Zones.Zip(zoneNames, (zone, name) =>
            BuildWorksheet(
                zone.DisplayName,
                data.ExportedAt,
                zone,
                name,
                zones: null)));

        return new ExcelDocument(worksheets);
    }

    private static ExcelWorksheetDefinition BuildWorksheet(
        string title,
        DateTime exportedAt,
        MetricsScopeData data,
        string worksheetName,
        IReadOnlyList<MetricsScopeData>? zones)
    {
        var composer = new WorksheetComposer();
        composer.AddBand(title, _reportTitleStyle);
        composer.AddBand($"Exported: {exportedAt:yyyy-MM-dd HH:mm}", null);
        composer.AddBlankRow();

        AddSummary(composer, data, zones is null ? "ZONE SUMMARY" : "PROJECT SUMMARY");
        composer.AddBlankRow();
        AddParcelUseSummary(composer, data.ParcelUses);
        composer.AddBlankRow();
        AddBuiltPlateUseSummary(composer, data.BuiltPlateUses);
        composer.AddBlankRow();
        AddTransitSummary(composer, data.TransitMetrics);

        if (zones is not null)
        {
            composer.AddBlankRow();
            AddZoneComparison(composer, zones);
        }

        return new ExcelWorksheetDefinition(
            worksheetName,
            [composer.Build()],
            new VerticalBlockPlacementStrategy(rowGap: 0),
            new ExcelCellAddress(row: 1, column: 1),
            new ExcelWorksheetOptions(
                HideGridlines: true,
                FrozenRowCount: 2,
                ColumnWidths: new Dictionary<int, double>
                {
                    [1] = 28,
                    [2] = 18,
                    [3] = 18,
                    [4] = 20,
                    [5] = 17,
                    [6] = 26,
                    [7] = 27,
                    [8] = 17,
                    [9] = 17,
                }));
    }

    private static void AddSummary(
        WorksheetComposer composer,
        MetricsScopeData data,
        string sectionTitle)
    {
        composer.AddBand(sectionTitle, _sectionTitleStyle);
        composer.AddRow(
            [
                ExcelCell.From("Metric"),
                ExcelCell.From("Value"),
                ExcelCell.From("Unit"),
            ],
            _headerStyle);

        if (data.ZoneBoundaryArea.HasValue)
        {
            composer.AddMetric(
                "Zone Boundary Area",
                data.ZoneBoundaryArea.Value,
                "units²",
                _numberStyle);
        }

        var parcelAreaCell = composer.AddMetric(
            data.ZoneBoundaryArea.HasValue ? "Assigned Parcel Area" : "Parcel Area",
            data.ParcelArea,
            "units²",
            _numberStyle);
        var footprintCell = composer.AddMetric(
            "Building Footprint",
            data.BuildingFootprint,
            "units²",
            _numberStyle);
        var builtAreaCell = composer.AddMetric(
            "Built Area",
            data.BuiltArea,
            "units²",
            _numberStyle);
        composer.AddMetric(
            "Transit Length",
            data.TransitLength,
            "units",
            _numberStyle);
        composer.AddMetric(
            "Transit Area",
            data.TransitArea,
            "units²",
            _numberStyle);
        composer.AddMetric(
            "Building Count",
            data.BuildingCount,
            "count",
            _integerStyle);
        composer.AddMetric(
            "Parcel Count",
            data.ParcelCount,
            "count",
            _integerStyle);
        composer.AddMetric(
            "Transit Count",
            data.TransitCount,
            "count",
            _integerStyle);
        composer.AddFormulaMetric(
            "Coverage",
            $"IF({parcelAreaCell}=0,0,{footprintCell}/{parcelAreaCell})",
            "ratio",
            _percentageStyle);
        composer.AddFormulaMetric(
            "FAR",
            $"IF({parcelAreaCell}=0,0,{builtAreaCell}/{parcelAreaCell})",
            "ratio",
            _ratioStyle);
    }

    private static void AddParcelUseSummary(
        WorksheetComposer composer,
        IReadOnlyList<ParcelUseData> sourceRows)
    {
        composer.AddBand("PARCEL USE SUMMARY", _sectionTitleStyle);
        composer.AddRow(
            [
                ExcelCell.From("Use"),
                ExcelCell.From("Colour"),
                ExcelCell.From("Parcel Count"),
                ExcelCell.From("Area"),
                ExcelCell.From("Area Share"),
                ExcelCell.From("Sum of Parcel Perimeters"),
                ExcelCell.From("Building Footprint on Parcels"),
                ExcelCell.From("Coverage"),
            ],
            _headerStyle);

        var rows = sourceRows
            .OrderBy(row => row.UseName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (rows.Count == 0)
        {
            composer.AddNoDataRow();
            return;
        }

        var firstDataRow = composer.NextRow;
        var totalRow = firstDataRow + rows.Count;

        foreach (var row in rows)
        {
            var currentRow = composer.NextRow;
            composer.AddRow([
                ExcelCell.From(row.UseName),
                ExcelCell.Blank(new ExcelCellStyle(FillColor: row.Color)),
                ExcelCell.From(row.ParcelCount, _integerStyle),
                ExcelCell.From(row.Area, _numberStyle),
                ExcelCell.FormulaA1(
                    $"IF(D{totalRow}=0,0,D{currentRow}/D{totalRow})",
                    _percentageStyle),
                ExcelCell.From(row.SumOfParcelPerimeters, _numberStyle),
                ExcelCell.From(row.BuildingFootprint, _numberStyle),
                ExcelCell.FormulaA1(
                    $"IF(D{currentRow}=0,0,G{currentRow}/D{currentRow})",
                    _percentageStyle),
            ]);
        }

        composer.AddRow(
            [
                ExcelCell.From("Total"),
                ExcelCell.Blank(),
                SumRange("C", firstDataRow, totalRow - 1, _integerStyle),
                SumRange("D", firstDataRow, totalRow - 1, _numberStyle),
                ExcelCell.FormulaA1(
                    $"IF(D{totalRow}=0,0,1)",
                    _percentageStyle),
                SumRange("F", firstDataRow, totalRow - 1, _numberStyle),
                SumRange("G", firstDataRow, totalRow - 1, _numberStyle),
                ExcelCell.FormulaA1(
                    $"IF(D{totalRow}=0,0,G{totalRow}/D{totalRow})",
                    _percentageStyle),
            ],
            _totalStyle);
    }

    private static void AddBuiltPlateUseSummary(
        WorksheetComposer composer,
        IReadOnlyList<BuiltPlateUseData> sourceRows)
    {
        composer.AddBand("BUILT-PLATE USE SUMMARY", _sectionTitleStyle);
        composer.AddRow(
            [
                ExcelCell.From("Floor-Plate Use"),
                ExcelCell.From("Colour"),
                ExcelCell.From("Built Area"),
                ExcelCell.From("Built Area Share"),
            ],
            _headerStyle);

        var rows = sourceRows
            .OrderBy(row => row.UseName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (rows.Count == 0)
        {
            composer.AddNoDataRow();
            return;
        }

        var firstDataRow = composer.NextRow;
        var totalRow = firstDataRow + rows.Count;

        foreach (var row in rows)
        {
            var currentRow = composer.NextRow;
            composer.AddRow([
                ExcelCell.From(row.UseName),
                ExcelCell.Blank(new ExcelCellStyle(FillColor: row.Color)),
                ExcelCell.From(row.BuiltArea, _numberStyle),
                ExcelCell.FormulaA1(
                    $"IF(C{totalRow}=0,0,C{currentRow}/C{totalRow})",
                    _percentageStyle),
            ]);
        }

        composer.AddRow(
            [
                ExcelCell.From("Total"),
                ExcelCell.Blank(),
                SumRange("C", firstDataRow, totalRow - 1, _numberStyle),
                ExcelCell.FormulaA1(
                    $"IF(C{totalRow}=0,0,1)",
                    _percentageStyle),
            ],
            _totalStyle);
    }

    private static void AddTransitSummary(
        WorksheetComposer composer,
        IReadOnlyList<TransitMetricData> sourceRows)
    {
        composer.AddBand("TRANSIT SUMMARY", _sectionTitleStyle);
        composer.AddRow(
            [
                ExcelCell.From("Type"),
                ExcelCell.From("Width"),
                ExcelCell.From("Corridor Count"),
                ExcelCell.From("Running Length"),
                ExcelCell.From("Length Share"),
                ExcelCell.From("Transit Area"),
                ExcelCell.From("Area Share"),
                ExcelCell.From("Colour"),
            ],
            _headerStyle);

        if (sourceRows.Count == 0)
        {
            composer.AddNoDataRow();
            return;
        }

        var groups = sourceRows
            .GroupBy(row => row.Type)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TransitTypeGroup(
                group.Key,
                group.GroupBy(row => row.Width)
                    .OrderBy(widthGroup => widthGroup.Key)
                    .Select(widthGroup => CreateTransitWidthGroup(
                        group.Key,
                        widthGroup.Key,
                        widthGroup))
                    .ToList()))
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
                        $"IF(D{grandTotalRow}=0,0,D{currentRow}/D{grandTotalRow})",
                        _percentageStyle),
                    ExcelCell.From(width.Area, _numberStyle),
                    ExcelCell.FormulaA1(
                        $"IF(F{grandTotalRow}=0,0,F{currentRow}/F{grandTotalRow})",
                        _percentageStyle),
                    ExcelCell.Blank(new ExcelCellStyle(FillColor: width.Color)),
                ]);
            }

            var typeTotalRow = composer.NextRow;
            typeTotalRows.Add(typeTotalRow);
            composer.AddRow(
                [
                    ExcelCell.From($"{group.Type} Total"),
                    ExcelCell.Blank(),
                    SumRange("C", firstDetailRow, typeTotalRow - 1, _integerStyle),
                    SumRange("D", firstDetailRow, typeTotalRow - 1, _numberStyle),
                    ExcelCell.FormulaA1(
                        $"IF(D{grandTotalRow}=0,0,D{typeTotalRow}/D{grandTotalRow})",
                        _percentageStyle),
                    SumRange("F", firstDetailRow, typeTotalRow - 1, _numberStyle),
                    ExcelCell.FormulaA1(
                        $"IF(F{grandTotalRow}=0,0,F{typeTotalRow}/F{grandTotalRow})",
                        _percentageStyle),
                    ExcelCell.Blank(),
                ],
                _subtotalStyle);
        }

        composer.AddRow(
            [
                ExcelCell.From("Grand Total"),
                ExcelCell.Blank(),
                SumCells("C", typeTotalRows, _integerStyle),
                SumCells("D", typeTotalRows, _numberStyle),
                ExcelCell.FormulaA1(
                    $"IF(D{grandTotalRow}=0,0,1)",
                    _percentageStyle),
                SumCells("F", typeTotalRows, _numberStyle),
                ExcelCell.FormulaA1(
                    $"IF(F{grandTotalRow}=0,0,1)",
                    _percentageStyle),
                ExcelCell.Blank(),
            ],
            _totalStyle);
    }

    private static TransitWidthGroup CreateTransitWidthGroup(
        string type,
        double width,
        IEnumerable<TransitMetricData> sourceRows)
    {
        var rows = sourceRows.ToList();
        var colors = rows.Select(row => row.Color).Distinct().ToList();
        if (colors.Count != 1)
        {
            throw new InvalidOperationException(
                $"Transit rows for {type} at width {width} have inconsistent colours.");
        }

        return new TransitWidthGroup(
            width,
            rows.Count,
            rows.Sum(row => row.RunningLength),
            rows.Sum(row => row.Area),
            colors.Single());
    }

    private static void AddZoneComparison(
        WorksheetComposer composer,
        IReadOnlyList<MetricsScopeData> zones)
    {
        composer.AddBand("ZONE COMPARISON", _sectionTitleStyle);
        composer.AddRow(
            [
                ExcelCell.From("Zone"),
                ExcelCell.From("Zone Boundary Area"),
                ExcelCell.From("Assigned Parcel Area"),
                ExcelCell.From("Building Footprint"),
                ExcelCell.From("Coverage"),
                ExcelCell.From("Built Area"),
                ExcelCell.From("FAR"),
                ExcelCell.From("Transit Length"),
                ExcelCell.From("Transit Area"),
            ],
            _headerStyle);

        if (zones.Count == 0)
        {
            composer.AddNoDataRow();
            return;
        }

        foreach (var zone in zones)
        {
            var currentRow = composer.NextRow;
            composer.AddRow([
                ExcelCell.From(zone.DisplayName),
                ExcelCell.From(zone.ZoneBoundaryArea ?? 0, _numberStyle),
                ExcelCell.From(zone.ParcelArea, _numberStyle),
                ExcelCell.From(zone.BuildingFootprint, _numberStyle),
                ExcelCell.FormulaA1(
                    $"IF(C{currentRow}=0,0,D{currentRow}/C{currentRow})",
                    _percentageStyle),
                ExcelCell.From(zone.BuiltArea, _numberStyle),
                ExcelCell.FormulaA1(
                    $"IF(C{currentRow}=0,0,F{currentRow}/C{currentRow})",
                    _ratioStyle),
                ExcelCell.From(zone.TransitLength, _numberStyle),
                ExcelCell.From(zone.TransitArea, _numberStyle),
            ]);
        }
    }

    private static ExcelCell SumRange(
        string column,
        int firstRow,
        int lastRow,
        ExcelCellStyle style)
    {
        return ExcelCell.FormulaA1(
            $"SUM({column}{firstRow}:{column}{lastRow})",
            style);
    }

    private static ExcelCell SumCells(
        string column,
        IReadOnlyList<int> rows,
        ExcelCellStyle style)
    {
        var references = string.Join(",", rows.Select(row => $"{column}{row}"));
        return ExcelCell.FormulaA1($"SUM({references})", style);
    }

    private static List<string> ResolveZoneWorksheetNames(
        IEnumerable<string> displayNames)
    {
        var resolvedNames = new List<string>();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ProjectWorksheetName,
        };

        foreach (var displayName in displayNames)
        {
            var baseName = SanitizeWorksheetName($"Zone - {displayName}");
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

    private sealed record TransitTypeGroup(
        string Type,
        IReadOnlyList<TransitWidthGroup> Widths);

    private sealed record TransitWidthGroup(
        double Width,
        int Count,
        double RunningLength,
        double Area,
        Color Color);

    private sealed class WorksheetComposer
    {
        private readonly TableBlockBuilder _builder = new();

        public int NextRow { get; private set; } = 1;

        public void AddBand(
            string text,
            ExcelCellStyle? style)
        {
            var cells = new List<ExcelCell> { ExcelCell.From(text) };
            while (cells.Count < ReportColumnCount)
                cells.Add(ExcelCell.Blank());

            AddRow(cells, style);
        }

        public string AddMetric(
            string name,
            object value,
            string unit,
            ExcelCellStyle valueStyle)
        {
            var row = NextRow;
            AddRow([
                ExcelCell.From(name),
                ExcelCell.From(value, valueStyle),
                ExcelCell.From(unit),
            ]);
            return $"$B${row}";
        }

        public void AddFormulaMetric(
            string name,
            string formula,
            string unit,
            ExcelCellStyle valueStyle)
        {
            AddRow([
                ExcelCell.From(name),
                ExcelCell.FormulaA1(formula, valueStyle),
                ExcelCell.From(unit),
            ]);
        }

        public void AddNoDataRow()
        {
            AddRow([ExcelCell.From("No data")]);
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
