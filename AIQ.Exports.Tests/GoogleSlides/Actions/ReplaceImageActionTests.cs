using AIQ.Exports.GoogleSlides;
using AIQ.Exports.GoogleSlides.Actions;
using AIQ.Exports.GoogleSlides.Filters;

namespace AIQ.Exports.Tests.GoogleSlides.Actions;

[Collection("GoogleSlidesIntegration")]
public class ReplaceImageActionTests
{
    [Fact]
    public async Task CanReplaceImageOnCopiedSlide()
    {
        var presentationId = GoogleSlidesConfig.PresentationId;
        var doc = new GoogleSlidesDocument(presentationId, GoogleSlidesConfig.ServiceAccountPath);

        var presentation = await doc.GetPresentationAsync();
        var firstSlideId = presentation.Slides.First().ObjectId;

        doc.AddAction(new CopySlideAction(firstSlideId));
        var copyResponse = await doc.ExecuteWithResponseAsync();
        var copiedSlideId = copyResponse.Replies?.FirstOrDefault()?.DuplicateObject?.ObjectId;
        Assert.NotNull(copiedSlideId);

        try
        {
            var imageUrl = "https://picsum.photos/200/100";
            var filter = new ImageFilter(copiedSlideId, "image_placeholder");
            doc.AddAction(new ReplaceImageAction(filter, imageUrl));
            var occurrencesChanged = await doc.ExecuteAsync();

            Assert.True(occurrencesChanged > 0);

#if DEBUG
            Console.WriteLine("=== MANUAL VERIFICATION REQUIRED ===");
            Console.WriteLine($"Copied Slide ID: {copiedSlideId}");
            Console.WriteLine("Please verify in Google Slides that the image was replaced");
            Console.WriteLine($"Presentation: https://docs.google.com/presentation/d/{presentationId}/edit");
            Console.WriteLine("Test will pause for 30 seconds for manual verification...");
            await Task.Delay(30000);
#endif
        }
        finally
        {
            doc.AddAction(new DeleteSlideAction(copiedSlideId!));
            await doc.ExecuteAsync();
        }
    }
}
