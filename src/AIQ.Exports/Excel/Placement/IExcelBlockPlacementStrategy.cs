using AIQ.Exports.Excel.Models;

namespace AIQ.Exports.Excel.Placement;

public interface IExcelBlockPlacementStrategy
{
    void Place(
        IReadOnlyList<IExcelBlock> blocks,
        ExcelCellAddress startingAddress);
}
