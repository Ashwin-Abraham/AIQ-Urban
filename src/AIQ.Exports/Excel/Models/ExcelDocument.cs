namespace AIQ.Exports.Excel.Models;

public sealed record ExcelDocument(
    IReadOnlyList<ExcelWorksheetDefinition> Worksheets);
