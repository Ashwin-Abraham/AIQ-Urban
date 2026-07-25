using System.Drawing;

namespace AIQ.Exports.Excel.Models;

public sealed record ExcelCellStyle(
    bool? Bold = null,
    double? FontSize = null,
    Color? FontColor = null,
    Color? FillColor = null,
    string? NumberFormat = null,
    ExcelHorizontalAlignment? HorizontalAlignment = null,
    ExcelVerticalAlignment? VerticalAlignment = null,
    bool? WrapText = null)
{
    public ExcelCellStyle Merge(ExcelCellStyle? incoming)
    {
        if (incoming is null)
            return this;

        return new ExcelCellStyle(
            Bold: incoming.Bold ?? Bold,
            FontSize: incoming.FontSize ?? FontSize,
            FontColor: incoming.FontColor ?? FontColor,
            FillColor: incoming.FillColor ?? FillColor,
            NumberFormat: incoming.NumberFormat ?? NumberFormat,
            HorizontalAlignment:
                incoming.HorizontalAlignment ?? HorizontalAlignment,
            VerticalAlignment:
                incoming.VerticalAlignment ?? VerticalAlignment,
            WrapText: incoming.WrapText ?? WrapText);
    }

    internal static ExcelCellStyle? Merge(
        ExcelCellStyle? existing,
        params ExcelCellStyle?[] incomingStyles)
    {
        var resolved = existing;

        foreach (var incoming in incomingStyles)
        {
            if (incoming is null)
                continue;

            resolved = resolved is null
                ? incoming
                : resolved.Merge(incoming);
        }

        return resolved;
    }
}
