namespace AIQ.Exports.Excel.Models;

public abstract record ExcelCellContent
{
    private ExcelCellContent()
    {
    }

    public sealed record Blank : ExcelCellContent;

    public sealed record Value(object Data) : ExcelCellContent;

    public sealed record Formula(
        string Expression,
        ExcelFormulaNotation Notation) : ExcelCellContent;
}
