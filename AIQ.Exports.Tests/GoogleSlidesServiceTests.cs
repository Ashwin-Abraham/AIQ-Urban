using System.Reflection;
using AIQ.Exports.GoogleSlides;

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

    [Fact]
    public async Task CanGetSlide()
    {
        var serviceAccountPath = GoogleSlidesConfig.ServiceAccountPath;
        var presentationId = GoogleSlidesConfig.PresentationId;
        var service = new GoogleSlidesDocument(presentationId, serviceAccountPath);
        var success = await service.ValidateAsync();

        Assert.True(success);
    }
}