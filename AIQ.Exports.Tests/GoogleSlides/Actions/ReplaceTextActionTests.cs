using AIQ.Exports.GoogleSlides;
using AIQ.Exports.GoogleSlides.Actions;
using AIQ.Exports.GoogleSlides.Utils;
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
            .WithTest(async (doc, copiedSlideNumber) =>
            {
                doc.AddAction(new ReplaceTextAction(copiedSlideNumber, "placeholder", TextMatchOption.DontMatchCase, "placeholder", "TEST_REPLACED"));
                var occurrencesChanged = await doc.ExecuteAsync();
                Assert.True(occurrencesChanged > 0);
            })
            .Run();
    }
}
