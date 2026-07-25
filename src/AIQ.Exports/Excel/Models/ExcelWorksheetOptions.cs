namespace AIQ.Exports.Excel.Models;

public sealed record ExcelWorksheetOptions(
    bool HideGridlines = false,
    int FrozenRowCount = 0,
    IReadOnlyDictionary<int, double>? ColumnWidths = null);
