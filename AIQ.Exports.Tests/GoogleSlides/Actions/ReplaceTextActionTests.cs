using AIQ.Exports.GoogleSlides;
using AIQ.Exports.GoogleSlides.Actions;
using AIQ.Exports.GoogleSlides.Filters;
using Google.Apis.Http;

namespace AIQ.Exports.Tests.GoogleSlides.Actions;

[Collection("GoogleSlidesIntegration")]
public class ReplaceTextActionTests
{
    [Fact]
    public async Task CanReplaceTextOnCopiedSlide()
    {
        await ActionTestPipeline
            .Create(GoogleSlidesConfig.PresentationId, ActionTestPipeline.CreateCredentialFromServiceAccount(GoogleSlidesConfig.ServiceAccountPath))
            .WithTest(async (doc, copiedSlideId) =>
            {
                var filter = new TextFilter(copiedSlideId, "placeholder", TextMatchOption.DontMatchCase);
                doc.AddAction(new ReplaceTextAction(filter, "placeholder", "TEST_REPLACED"));
                var occurrencesChanged = await doc.ExecuteAsync();
                Assert.True(occurrencesChanged > 0);
            })
            .Run();
    }
}
