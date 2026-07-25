using System.Globalization;
using AIQ.Exports.Excel.Models;
using ClosedXML.Excel;

namespace AIQ.Exports.Excel.Writing;

internal sealed class TableBlockWriter : IExcelBlockWriter
{
    public bool CanWrite(IExcelBlock block)
    {
        return block is TableBlock;
    }

    public void Write(
        IXLWorksheet worksheet,
        IExcelBlock block)
    {
        ArgumentNullException.ThrowIfNull(worksheet);

        if (block is not TableBlock tableBlock)
            throw new ArgumentException(
                $"Blocks of type {block.GetType().FullName} are not supported.",
                nameof(block));

        var seed = tableBlock.SeedCellAddress
            ?? throw new InvalidOperationException(
                "The block must be placed before it can be written.");

        for (var rowIndex = 0; rowIndex < tableBlock.RowCount; rowIndex++)
        {
            for (var columnIndex = 0;
                 columnIndex < tableBlock.ColumnCount;
                 columnIndex++)
            {
                var source = tableBlock.Cells[rowIndex, columnIndex];
                if (source.Content is ExcelCellContent.Blank
                    && source.Style is null)
                {
                    continue;
                }

                var target = worksheet.Cell(
                    seed.Row + rowIndex,
                    seed.Column + columnIndex);
                WriteContent(target, source.Content);
                ApplyStyle(target, source.Style);
            }
        }
    }

    private static void WriteContent(
        IXLCell target,
        ExcelCellContent content)
    {
        switch (content)
        {
            case ExcelCellContent.Blank:
                break;
            case ExcelCellContent.Formula
                {
                    Notation: ExcelFormulaNotation.A1,
                } formula:
                target.FormulaA1 = formula.Expression;
                break;
            case ExcelCellContent.Formula formula:
                target.FormulaR1C1 = formula.Expression;
                break;
            case ExcelCellContent.Value value:
                WriteValue(target, value.Data);
                break;
        }
    }

    private static void WriteValue(
        IXLCell target,
        object value)
    {
        switch (value)
        {
            case string text:
                target.Value = text;
                return;
            case bool boolean:
                target.Value = boolean;
                return;
            case DateTime dateTime:
                target.Value = dateTime;
                return;
            case TimeSpan timeSpan:
                target.Value = timeSpan;
                return;
            default:
                target.Value = Convert.ToDouble(
                    value,
                    CultureInfo.InvariantCulture);
                return;
        }
    }

    private static void ApplyStyle(
        IXLCell target,
        ExcelCellStyle? style)
    {
        if (style is null)
            return;

        if (style.Bold.HasValue)
            target.Style.Font.Bold = style.Bold.Value;

        if (style.FontSize.HasValue)
            target.Style.Font.FontSize = style.FontSize.Value;

        if (style.FontColor.HasValue)
        {
            target.Style.Font.FontColor =
                XLColor.FromColor(style.FontColor.Value);
        }

        if (style.FillColor.HasValue)
        {
            target.Style.Fill.SetBackgroundColor(
                XLColor.FromColor(style.FillColor.Value));
        }

        if (style.NumberFormat is not null)
            target.Style.NumberFormat.Format = style.NumberFormat;

        if (style.HorizontalAlignment.HasValue)
        {
            target.Style.Alignment.Horizontal = style.HorizontalAlignment.Value
                switch
                {
                    ExcelHorizontalAlignment.General =>
                        XLAlignmentHorizontalValues.General,
                    ExcelHorizontalAlignment.Left =>
                        XLAlignmentHorizontalValues.Left,
                    ExcelHorizontalAlignment.Center =>
                        XLAlignmentHorizontalValues.Center,
                    ExcelHorizontalAlignment.Right =>
                        XLAlignmentHorizontalValues.Right,
                    ExcelHorizontalAlignment.Fill =>
                        XLAlignmentHorizontalValues.Fill,
                    ExcelHorizontalAlignment.Justify =>
                        XLAlignmentHorizontalValues.Justify,
                    ExcelHorizontalAlignment.CenterContinuous =>
                        XLAlignmentHorizontalValues.CenterContinuous,
                    ExcelHorizontalAlignment.Distributed =>
                        XLAlignmentHorizontalValues.Distributed,
                    _ => throw new ArgumentOutOfRangeException(nameof(style)),
                };
        }

        if (style.VerticalAlignment.HasValue)
        {
            target.Style.Alignment.Vertical = style.VerticalAlignment.Value
                switch
                {
                    ExcelVerticalAlignment.Top =>
                        XLAlignmentVerticalValues.Top,
                    ExcelVerticalAlignment.Center =>
                        XLAlignmentVerticalValues.Center,
                    ExcelVerticalAlignment.Bottom =>
                        XLAlignmentVerticalValues.Bottom,
                    ExcelVerticalAlignment.Justify =>
                        XLAlignmentVerticalValues.Justify,
                    ExcelVerticalAlignment.Distributed =>
                        XLAlignmentVerticalValues.Distributed,
                    _ => throw new ArgumentOutOfRangeException(nameof(style)),
                };
        }

        if (style.WrapText.HasValue)
            target.Style.Alignment.WrapText = style.WrapText.Value;
    }
}
