namespace AIQ.Exports.Excel.Models;

public sealed class TableBlock : IExcelBlock
{
    public ExcelCell[,] Cells { get; }

    public int RowCount => Cells.GetLength(0);

    public int ColumnCount => Cells.GetLength(1);

    public ExcelCellAddress? SeedCellAddress { get; set; }

    internal TableBlock(ExcelCell[,] cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        Cells = cells;
    }
}
