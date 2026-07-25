namespace AIQ.Exports.Excel.Models;

public sealed record ExcelCell
{
    public ExcelCellContent Content { get; private set; }

    public ExcelCellStyle? Style { get; set; }

    private ExcelCell(
        ExcelCellContent content,
        ExcelCellStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        Content = content;
        Style = style;
    }

    public static ExcelCell Blank(ExcelCellStyle? style = null)
    {
        return new(new ExcelCellContent.Blank(), style);
    }

    public static ExcelCell From(
        object data,
        ExcelCellStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (!IsSupportedScalar(data))
            throw new ArgumentException(
                $"Values of type {data.GetType().FullName} are not supported.",
                nameof(data));

        return new(new ExcelCellContent.Value(data), style);
    }

    public static ExcelCell FormulaA1(
        string expression,
        ExcelCellStyle? style = null)
    {
        return Formula(expression, ExcelFormulaNotation.A1, style);
    }

    public static ExcelCell FormulaR1C1(
        string expression,
        ExcelCellStyle? style = null)
    {
        return Formula(expression, ExcelFormulaNotation.R1C1, style);
    }

    internal ExcelCell Copy()
    {
        return new(Content, Style);
    }

    private static ExcelCell Formula(
        string expression,
        ExcelFormulaNotation notation,
        ExcelCellStyle? style)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException(
                "A formula expression cannot be null or whitespace.",
                nameof(expression));

        return new(
            new ExcelCellContent.Formula(expression, notation),
            style);
    }

    private static bool IsSupportedScalar(object data)
    {
        return data is string
            or sbyte
            or byte
            or short
            or ushort
            or int
            or uint
            or long
            or ulong
            or float
            or double
            or decimal
            or bool
            or DateTime
            or TimeSpan;
    }
}
