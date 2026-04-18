using System.Reflection;

namespace AIQ.Exports.Tests;

public class GoogleSlidesServiceTests
{
    [Fact]
    public void ServiceClass_ShouldExist()
    {
        var type = typeof(GoogleSlides.GoogleSlidesService);
        Assert.NotNull(type);
    }

    [Fact]
    public void ConfigClass_ShouldExist()
    {
        var type = typeof(GoogleSlides.GoogleSlidesConfig);
        Assert.NotNull(type);
        Assert.True(type.IsSealed);
        Assert.True(type.IsAbstract);
    }
}