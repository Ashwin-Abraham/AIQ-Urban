namespace AIQ.Exports.Excel.Models;

public interface IExcelBlock
{
    int RowCount { get; }

    int ColumnCount { get; }

    ExcelCellAddress? SeedCellAddress { get; set; }
}
