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
        var commonToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

        var success = await service.ValidateAsync(commonToken);

        Assert.True(success);
    }

    [Fact]
    public async Task CanCopyPresentation()
    {
        var serviceAccountPath = GoogleSlidesConfig.ServiceAccountPath;
        var presentationId = GoogleSlidesConfig.PresentationId;
        var service = new GoogleSlidesDocument(presentationId, serviceAccountPath);
        var commonToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

        var success = await service.CopyPresentationAsync("test copy", commonToken);

        Assert.True(success != null);
    }


}
