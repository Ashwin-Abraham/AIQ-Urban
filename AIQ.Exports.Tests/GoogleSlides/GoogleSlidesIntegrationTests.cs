using AIQ.Exports.GoogleSlides;

namespace AIQ.Exports.Tests.GoogleSlides;

public class GoogleSlidesIntegrationTests
{
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
