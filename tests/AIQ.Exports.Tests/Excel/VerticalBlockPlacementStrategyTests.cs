using AIQ.Exports.Excel.Builders;
using AIQ.Exports.Excel.Models;
using AIQ.Exports.Excel.Placement;

namespace AIQ.Exports.Tests.Excel;

public class VerticalBlockPlacementStrategyTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    public void Constructor_NonPositiveCoordinate_ThrowsArgumentOutOfRangeException(
        int row,
        int column)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ExcelCellAddress(row, column));
    }

    [Fact]
    public void Place_Blocks_AssignsAddressesWithConfiguredGap()
    {
        IExcelBlock[] blocks =
        [
            BuildBlock(rowCount: 3),
            BuildBlock(rowCount: 4),
        ];
        var strategy = new VerticalBlockPlacementStrategy(rowGap: 2);

        strategy.Place(blocks, new ExcelCellAddress(row: 2, column: 3));

        Assert.Equal(
            new ExcelCellAddress(row: 2, column: 3),
            blocks[0].SeedCellAddress);
        Assert.Equal(
            new ExcelCellAddress(row: 7, column: 3),
            blocks[1].SeedCellAddress);
    }

    [Fact]
    public void Place_EmptyBlocks_PerformsNoWork()
    {
        var strategy = new VerticalBlockPlacementStrategy(rowGap: 2);

        strategy.Place([], new ExcelCellAddress(row: 1, column: 1));
    }

    [Fact]
    public void Constructor_NegativeGap_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new VerticalBlockPlacementStrategy(rowGap: -1));
    }

    [Fact]
    public void Place_BlockWouldExceedWorksheetBounds_ThrowsArgumentOutOfRangeException()
    {
        IExcelBlock[] blocks = [BuildBlock(rowCount: 2)];
        var strategy = new VerticalBlockPlacementStrategy(rowGap: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            strategy.Place(
                blocks,
                new ExcelCellAddress(
                    row: ExcelCellAddress.MaximumRow,
                    column: 1)));
    }

    private static TableBlock BuildBlock(int rowCount)
    {
        var builder = new TableBlockBuilder();

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            builder.AddNewRow([ExcelCell.From(rowIndex)]);

        return builder.Build();
    }
}
