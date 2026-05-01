using AIQ.Exports.GoogleSlides;
using AIQ.Exports.GoogleSlides.Actions;

namespace AIQ.Exports.Tests.GoogleSlides.Actions;

[Collection("GoogleSlidesIntegration")]
public class CopyAndDeleteSlideActionTests
{
    [Fact]
    public async Task CanCopySlide()
    {
        var serviceAccountPath = GoogleSlidesConfig.ServiceAccountPath;
        var presentationId = GoogleSlidesConfig.PresentationId;
        var doc = new GoogleSlidesDocument(presentationId, serviceAccountPath);

        // Get initial slide count and first slide ID
        var presentation = await doc.GetPresentationAsync();
        var initialSlideCount = presentation.Slides.Count;
        var firstSlideId = presentation.Slides.First().ObjectId;

        // Copy slide 1
        doc.AddAction(new CopySlideAction(firstSlideId));
        var response = await doc.ExecuteWithResponseAsync();

        // Verify new slide created
        var newSlideId = response.Replies?.FirstOrDefault()?.DuplicateObject?.ObjectId;
        Assert.NotNull(newSlideId);

        // Verify slide count increased
        presentation = await doc.GetPresentationAsync();
        Assert.Equal(initialSlideCount + 1, presentation.Slides.Count);

#if DEBUG
        // OUTPUT FOR MANUAL VERIFICATION
        Console.WriteLine("=== MANUAL VERIFICATION REQUIRED ===");
        Console.WriteLine($"Copied Slide ID: {newSlideId}");
        Console.WriteLine("Please verify in Google Slides that the slide was copied");
        Console.WriteLine($"Presentation: https://docs.google.com/presentation/d/{presentationId}/edit");

        // PAUSE FOR MANUAL VERIFICATION (only in DEBUG builds - 30 seconds to verify)
        Console.WriteLine("Test will pause for 30 seconds for manual verification...");
        await Task.Delay(30000);
#endif

        // Cleanup: delete the copied slide
        doc.AddAction(new DeleteSlideAction(newSlideId!));
        await doc.ExecuteAsync();

        // Verify slide count restored
        presentation = await doc.GetPresentationAsync();
        Assert.Equal(initialSlideCount, presentation.Slides.Count);
    }
}
