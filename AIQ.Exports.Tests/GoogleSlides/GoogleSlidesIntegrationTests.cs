using AIQ.Exports.GoogleSlides;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;

namespace AIQ.Exports.Tests.GoogleSlides;

public class GoogleSlidesIntegrationTests
{
    private static IConfigurableHttpClientInitializer CreateTestCredential()
    {
        string json = File.ReadAllText(GoogleSlidesConfig.ServiceAccountPath);
        return GoogleCredential.FromJson(json)
            .CreateScoped(GoogleSlidesConstants.SlidesScope, GoogleSlidesConstants.DriveScope);
    }

    [Fact]
    public async Task CanGetSlide()
    {
        var presentationId = GoogleSlidesConfig.PresentationId;
        var service = new GoogleSlidesDocument(presentationId, CreateTestCredential());
        var commonToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

        var success = await service.ValidateAsync(commonToken);

        Assert.True(success);
    }

    [Fact]
    public async Task CanCopyPresentation()
    {
        var presentationId = GoogleSlidesConfig.PresentationId;
        var service = new GoogleSlidesDocument(presentationId, CreateTestCredential());
        var commonToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

        var success = await service.CopyPresentationAsync("test copy", commonToken);

        Assert.True(success != null);
    }


}
