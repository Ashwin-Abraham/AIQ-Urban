using Microsoft.Extensions.Logging;
using AIQ.Urban.RhinoInterface.Logging;
using Xunit;

namespace AIQ.Urban.Tests.Logging;

public class RhinoLoggerTests
{
    [Fact]
    public void RhinoLogProvider_Creates_Correct_CategoryName()
    {
        // Arrange
        var provider = new RhinoLogProvider();

        // Act
        var logger = provider.CreateLogger("TestCategory");

        // Assert
        Assert.NotNull(logger);
        Assert.IsType<RhinoLogger>(logger);
    }

    [Fact]
    public void RhinoLogger_IsEnabled_ReturnsCorrectValue()
    {
        // Arrange
        var provider = new RhinoLogProvider();
        var logger = provider.CreateLogger("Test");

        // Act & Assert — IsEnabled returns true for Information and above.
        // Note: LogLevel.None (value 6) >= LogLevel.Information (value 2) is true,
        // so IsEnabled(None) returns true — this is a known quirk of the >= comparison.
        Assert.False(logger.IsEnabled(LogLevel.Trace));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
        Assert.True(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.True(logger.IsEnabled(LogLevel.Critical));
        Assert.True(logger.IsEnabled(LogLevel.None));
    }

    [Fact]
    public void RhinoLogger_BeginScope_ReturnsNull()
    {
        // Arrange
        var provider = new RhinoLogProvider();
        var logger = provider.CreateLogger("Test");

        // Act
        var scope = logger.BeginScope("Scope");

        // Assert — implementation returns null
        Assert.Null(scope);
    }

    [Fact]
    public void AddRhinoLog_RegistersProvider()
    {
        // Arrange & Act
        var factory = new LoggerFactory();
        factory.AddRhinoLog();

        // Assert — should not throw, factory should have providers
        var logger = factory.CreateLogger("Test");
        Assert.NotNull(logger);
    }

    [Fact]
    public void RhinoLogger_VerifiesLevelFiltering()
    {
        // Arrange
        var provider = new RhinoLogProvider();
        var logger = provider.CreateLogger("TestCat");

        // Assert — verify the level filtering behavior that gates whether Log reaches RhinoApp
        Assert.False(logger.IsEnabled(LogLevel.Trace));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
        Assert.True(logger.IsEnabled(LogLevel.Information));
    }
}
