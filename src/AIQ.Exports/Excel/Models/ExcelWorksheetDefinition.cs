using AIQ.Exports.Excel.Placement;

namespace AIQ.Exports.Excel.Models;

public sealed record ExcelWorksheetDefinition(
    string Name,
    IReadOnlyList<IExcelBlock> Blocks,
    IExcelBlockPlacementStrategy PlacementStrategy,
    ExcelCellAddress StartingAddress);
