using AIQ.Exports.GoogleSlides;
using AIQ.Exports.GoogleSlides.Actions;
using AIQ.Exports.GoogleSlides.Filters;

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

    [Fact]
    public async Task CanReplaceTextOnCopiedSlide()
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
            var filter = new TextFilter(copiedSlideId, "placeholder", TextMatchOption.DontMatchCase);
            doc.AddAction(new ReplaceTextAction(filter, "placeholder", "TEST_REPLACED"));
            var occurrencesChanged = await doc.ExecuteAsync();

            Assert.True(occurrencesChanged > 0);

#if DEBUG
            Console.WriteLine("=== MANUAL VERIFICATION REQUIRED ===");
            Console.WriteLine($"Copied Slide ID: {copiedSlideId}");
            Console.WriteLine("Please verify in Google Slides that text was replaced to 'TEST_REPLACED'");
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
