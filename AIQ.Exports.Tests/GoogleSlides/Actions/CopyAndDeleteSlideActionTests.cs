using AIQ.Exports.GoogleSlides;
using AIQ.Exports.GoogleSlides.Actions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;

namespace AIQ.Exports.Tests.GoogleSlides.Actions;

[Collection("GoogleSlidesIntegration")]
public class CopyAndDeleteSlideActionTests
{
    private static IConfigurableHttpClientInitializer CreateTestCredential()
    {
        string json = File.ReadAllText(GoogleSlidesConfig.ServiceAccountPath);
        return GoogleCredential.FromJson(json)
            .CreateScoped(GoogleSlidesConstants.SlidesScope, GoogleSlidesConstants.DriveScope);
    }

    [Fact]
    public async Task CanCopySlide()
    {
        var presentationId = GoogleSlidesConfig.PresentationId;
        var doc = new GoogleSlidesDocument(presentationId, CreateTestCredential());

        var presentation = await doc.GetPresentationAsync();
        var initialSlideCount = presentation.Slides.Count;
        var firstSlideId = presentation.Slides.First().ObjectId;

        doc.AddAction(new CopySlideAction(firstSlideId));
        var response = await doc.ExecuteWithResponseAsync();

        var newSlideId = response.Replies?.FirstOrDefault()?.DuplicateObject?.ObjectId;
        Assert.NotNull(newSlideId);

        presentation = await doc.GetPresentationAsync();
        Assert.Equal(initialSlideCount + 1, presentation.Slides.Count);

#if DEBUG
        Console.WriteLine("=== MANUAL VERIFICATION REQUIRED ===");
        Console.WriteLine($"Copied Slide ID: {newSlideId}");
        Console.WriteLine("Please verify in Google Slides that the slide was copied");
        Console.WriteLine($"Presentation: https://docs.google.com/presentation/d/{presentationId}/edit");
        Console.WriteLine("Test will pause for 30 seconds for manual verification...");
        await Task.Delay(30000);
#endif

        doc.AddAction(new DeleteSlideAction(newSlideId!));
        await doc.ExecuteAsync();

        presentation = await doc.GetPresentationAsync();
        Assert.Equal(initialSlideCount, presentation.Slides.Count);
    }
}
