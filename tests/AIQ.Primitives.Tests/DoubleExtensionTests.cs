using AIQ.Primitives.Extensions;

namespace AIQ.Primitives.Tests;

public class DoubleExtensionTests
{
    [Theory]
    [InlineData(345.67, "345.67")]
    [InlineData(123345.67, "123,345.67")]
    [InlineData(123345.67, "123345.67", false)]
    [InlineData(123345.00, "123,345.00")]
    [InlineData(0, "0.00")]
    [InlineData(023, "23.00")]
    public void ToFormattedTextTests(double number, string expectedNumber, bool withCommas = true)
    {
        var result = number.ToFormattedText(withCommas, decimals: 2);

        Assert.Equal(expectedNumber, result);
    }
}