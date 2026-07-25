namespace AIQ.Exports.Excel.Models;

public readonly record struct ExcelCellAddress
{
    public const int MaximumRow = 1_048_576;
    public const int MaximumColumn = 16_384;

    public int Row { get; }

    public int Column { get; }

    public ExcelCellAddress(int row, int column)
    {
        if (row < 1 || row > MaximumRow)
            throw new ArgumentOutOfRangeException(nameof(row));

        if (column < 1 || column > MaximumColumn)
            throw new ArgumentOutOfRangeException(nameof(column));

        Row = row;
        Column = column;
    }
}
