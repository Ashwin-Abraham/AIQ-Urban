using AIQ.Exports.Excel.Models;

namespace AIQ.Exports.Tests.Excel;

public class ExcelCellTests
{
    [Theory]
    [InlineData("text")]
    [InlineData(42)]
    [InlineData(12.5)]
    [InlineData(true)]
    public void From_SupportedScalar_PreservesTypedValue(object value)
    {
        var cell = ExcelCell.From(value);

        var content = Assert.IsType<ExcelCellContent.Value>(cell.Content);
        Assert.Equal(value, content.Data);
    }

    [Fact]
    public void From_UnsupportedObject_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ExcelCell.From(new Version(1, 0)));
    }

    [Fact]
    public void Blank_CreatesBlankContent()
    {
        var cell = ExcelCell.Blank();

        Assert.IsType<ExcelCellContent.Blank>(cell.Content);
    }

    [Fact]
    public void FormulaA1_Expression_CreatesA1Formula()
    {
        var cell = ExcelCell.FormulaA1("SUM(A1:A3)");

        var formula = Assert.IsType<ExcelCellContent.Formula>(cell.Content);
        Assert.Equal("SUM(A1:A3)", formula.Expression);
        Assert.Equal(ExcelFormulaNotation.A1, formula.Notation);
    }

    [Fact]
    public void FormulaR1C1_Expression_CreatesR1C1Formula()
    {
        var cell = ExcelCell.FormulaR1C1("SUM(R[-3]C:R[-1]C)");

        var formula = Assert.IsType<ExcelCellContent.Formula>(cell.Content);
        Assert.Equal("SUM(R[-3]C:R[-1]C)", formula.Expression);
        Assert.Equal(ExcelFormulaNotation.R1C1, formula.Notation);
    }
}
