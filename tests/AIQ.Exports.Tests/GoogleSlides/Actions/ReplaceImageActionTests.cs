using AIQ.Exports.GoogleSlides;
using AIQ.Exports.GoogleSlides.Actions;
using Google.Apis.Http;

namespace AIQ.Exports.Tests.GoogleSlides.Actions;

[Collection("GoogleSlidesIntegration")]
public class ReplaceImageActionTests
{
    [Fact]
    public async Task CanReplaceImageOnCopiedSlide()
    {
        await ActionTestPipeline
            .Create(GoogleSlidesConfig.PresentationId, ActionTestPipeline.CreateCredentialFromServiceAccount(GoogleSlidesConfig.ServiceAccountPath))
            .WithTest(async (doc, copiedSlideNumber) =>
            {
                var imageUrl = "https://picsum.photos/200/100";
                doc.AddAction(new ReplaceImageAction(copiedSlideNumber, "image_placeholder", imageUrl));
                var occurrencesChanged = await doc.ExecuteAsync();
                Assert.True(occurrencesChanged > 0);
            })
            .Run();
    }
}
