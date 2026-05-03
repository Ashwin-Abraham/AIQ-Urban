using AIQ.Exports.GoogleSlides;
using AIQ.Exports.GoogleSlides.Actions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;

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

        var presentation = await doc.GetPresentationAsync();
        var firstSlideId = presentation.Slides.First().ObjectId;

        doc.AddAction(new CopySlideAction(firstSlideId));
        var response = await doc.ExecuteWithResponseAsync();
        var copiedSlideId = response.Replies?.FirstOrDefault()?.DuplicateObject?.ObjectId;
        if (copiedSlideId == null) throw new InvalidOperationException("Failed to copy slide");

        // Get updated presentation to find the copied slide's number
        var updatedPresentation = await doc.GetPresentationAsync();
        int? copiedSlideNumber = null;
        for (int i = 0; i < updatedPresentation.Slides.Count; i++)
        {
            if (updatedPresentation.Slides[i].ObjectId == copiedSlideId)
            {
                copiedSlideNumber = i + 1;
                break;
            }
        }
        if (copiedSlideNumber == null) throw new InvalidOperationException("Failed to find copied slide in presentation");

        try
        {
            if (_testAction == null) throw new InvalidOperationException("Test action not configured");
            await _testAction(doc, copiedSlideNumber.Value);
        }
        finally
        {
            doc.AddAction(new DeleteSlideAction(copiedSlideId));
            await doc.ExecuteAsync();
        }
    }
}
