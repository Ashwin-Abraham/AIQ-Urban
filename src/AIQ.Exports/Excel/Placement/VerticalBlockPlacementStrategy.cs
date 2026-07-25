using AIQ.Exports.Excel.Models;

namespace AIQ.Exports.Excel.Placement;

public sealed class VerticalBlockPlacementStrategy
    : IExcelBlockPlacementStrategy
{
    private readonly int _rowGap;

    public VerticalBlockPlacementStrategy(int rowGap)
    {
        if (rowGap < 0)
            throw new ArgumentOutOfRangeException(nameof(rowGap));

        _rowGap = rowGap;
    }

    public void Place(
        IReadOnlyList<IExcelBlock> blocks,
        ExcelCellAddress startingAddress)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        long nextRow = startingAddress.Row;

        foreach (var block in blocks)
        {
            ArgumentNullException.ThrowIfNull(block);

            var lastRow = nextRow + Math.Max(block.RowCount - 1, 0);
            var lastColumn = (long)startingAddress.Column
                + Math.Max(block.ColumnCount - 1, 0);

            if (nextRow > ExcelCellAddress.MaximumRow
                || lastRow > ExcelCellAddress.MaximumRow
                || lastColumn > ExcelCellAddress.MaximumColumn)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(blocks),
                    "The placed block would exceed Excel worksheet bounds.");
            }

            block.SeedCellAddress = new ExcelCellAddress(
                (int)nextRow,
                startingAddress.Column);
            nextRow += block.RowCount + _rowGap;
        }
    }
}
