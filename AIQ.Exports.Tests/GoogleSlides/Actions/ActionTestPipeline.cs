using AIQ.Exports.GoogleSlides;
using AIQ.Exports.GoogleSlides.Actions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.Tests.GoogleSlides.Actions;

public class ActionTestPipeline
{
    private readonly string _presentationId;
    private readonly IConfigurableHttpClientInitializer _credential;
    private Func<GoogleSlidesDocument, int, Task>? _testAction;

    private ActionTestPipeline(string presentationId, IConfigurableHttpClientInitializer credential)
    {
        _presentationId = presentationId;
        _credential = credential;
    }

    public static ActionTestPipeline Create(string presentationId, string serviceAccountPath)
        => new ActionTestPipeline(presentationId, CreateCredentialFromServiceAccount(serviceAccountPath));

    public static ActionTestPipeline Create(string presentationId, IConfigurableHttpClientInitializer credential)
        => new ActionTestPipeline(presentationId, credential);

    public static IConfigurableHttpClientInitializer CreateCredentialFromServiceAccount(string path)
    {
        string json = File.ReadAllText(path);
        return GoogleCredential.FromJson(json)
            .CreateScoped(GoogleSlidesConstants.SlidesScope, GoogleSlidesConstants.DriveScope);
    }

    public ActionTestPipeline WithTest(Func<GoogleSlidesDocument, int, Task> testAction)
    {
        _testAction = testAction;
        return this;
    }

    public async Task Run()
    {
        var doc = new GoogleSlidesDocument(_presentationId, _credential);
        int slideToCopy = 0;

        // Copy Slide 1
        var copiedSlideId = await CopySlide(doc, slideToCopy);

        // Get updated presentation to find the copied slide's number
        var updatedPresentation = await doc.GetPresentationAsync();
        var copiedSlideIndex = slideToCopy + 1;
        
        try
        {
            if (_testAction == null) throw new InvalidOperationException("Test action not configured");
            await _testAction(doc, copiedSlideIndex);
        }
        finally
        {
            doc.AddAction(new DeleteSlideAction(copiedSlideId));
            await doc.ExecuteAsync();
        }
    }

    private static async Task<string?> CopySlide(GoogleSlidesDocument doc, int number = 0)
    {
        var presentation = await doc.GetPresentationAsync();

        var slideId = presentation.Slides.ElementAtOrDefault(number)?.ObjectId;

        doc.AddAction(new CopySlideAction(slideId));
        var response = await doc.ExecuteWithResponseAsync();

        var copiedSlideId = response.Replies?.FirstOrDefault()?.DuplicateObject?.ObjectId;

        return copiedSlideId;
    }
}
