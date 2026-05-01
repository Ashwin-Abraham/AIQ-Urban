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
        await ActionTestPipeline
            .Create(GoogleSlidesConfig.PresentationId, GoogleSlidesConfig.ServiceAccountPath)
            .WithTest(async (doc, copiedSlideId) =>
            {
                var imageUrl = "https://picsum.photos/200/100";
                var filter = new ImageFilter(copiedSlideId, "image_placeholder");
                doc.AddAction(new ReplaceImageAction(filter, imageUrl));
                var occurrencesChanged = await doc.ExecuteAsync();
                Assert.True(occurrencesChanged > 0);
            })
            .Run();
    }
}
