using AIQ.Exports.Excel.Models;

namespace AIQ.Exports.Excel.Builders;

public sealed class TableBlockBuilder
{
    private readonly List<List<ExcelCell>> _rows = [];

    public TableBlockBuilder AddNewRow(
        IEnumerable<ExcelCell> data,
        ExcelCellStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        var row = data
            .Select(cell =>
            {
                var copy = cell.Copy();
                copy.Style = ExcelCellStyle.Merge(style, copy.Style);
                return copy;
            })
            .ToList();

        _rows.Add(row);
        return this;
    }

    public TableBlockBuilder ModifyColumn(
        int columnIndex,
        IEnumerable<ExcelCell> data,
        bool overwriteExistingData,
        ExcelCellStyle? style = null)
    {
        if (columnIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(columnIndex));

        ArgumentNullException.ThrowIfNull(data);
        var incomingCells = data
            .Select(cell => cell.Copy())
            .ToList();

        while (_rows.Count < incomingCells.Count)
            _rows.Add([]);

        for (var rowIndex = 0; rowIndex < incomingCells.Count; rowIndex++)
        {
            var row = _rows[rowIndex];
            while (row.Count <= columnIndex)
                row.Add(ExcelCell.Blank());

            if (overwriteExistingData
                || row[columnIndex].Content is ExcelCellContent.Blank)
            {
                var existingStyle = row[columnIndex].Style;
                var incoming = incomingCells[rowIndex];
                incoming.Style = ExcelCellStyle.Merge(
                    existingStyle,
                    incoming.Style,
                    style);
                row[columnIndex] = incoming;
            }
        }

        return this;
    }

    public TableBlockBuilder SetRowStyle(
        int rowIndex,
        ExcelCellStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);

        if (rowIndex < 0 || rowIndex >= _rows.Count)
            throw new ArgumentOutOfRangeException(nameof(rowIndex));

        foreach (var cell in _rows[rowIndex])
            cell.Style = ExcelCellStyle.Merge(cell.Style, style);

        return this;
    }

    public TableBlock Build()
    {
        var rowCount = _rows.Count;
        var columnCount = _rows.Count == 0
            ? 0
            : _rows.Max(row => row.Count);
        var cells = new ExcelCell[rowCount, columnCount];

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                cells[rowIndex, columnIndex] = columnIndex < _rows[rowIndex].Count
                    ? _rows[rowIndex][columnIndex].Copy()
                    : ExcelCell.Blank();
            }
        }

        return new TableBlock(cells);
    }
}
