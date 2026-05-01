using AIQ.Exports.GoogleSlides;
using AIQ.Exports.GoogleSlides.Actions;

namespace AIQ.Exports.Tests.GoogleSlides.Actions;

public class ActionTestPipeline
{
    private readonly string _presentationId;
    private readonly string _serviceAccountPath;
    private Func<GoogleSlidesDocument, string, Task>? _testAction;

    private ActionTestPipeline(string presentationId, string serviceAccountPath)
    {
        _presentationId = presentationId;
        _serviceAccountPath = serviceAccountPath;
    }

    public static ActionTestPipeline Create(string presentationId, string serviceAccountPath)
        => new ActionTestPipeline(presentationId, serviceAccountPath);

    public ActionTestPipeline WithTest(Func<GoogleSlidesDocument, string, Task> testAction)
    {
        _testAction = testAction;
        return this;
    }

    public async Task Run()
    {
        var doc = new GoogleSlidesDocument(_presentationId, _serviceAccountPath);

        var presentation = await doc.GetPresentationAsync();
        var firstSlideId = presentation.Slides.First().ObjectId;

        doc.AddAction(new CopySlideAction(firstSlideId));
        var response = await doc.ExecuteWithResponseAsync();
        var copiedSlideId = response.Replies?.FirstOrDefault()?.DuplicateObject?.ObjectId;
        if (copiedSlideId == null) throw new InvalidOperationException("Failed to copy slide");

        try
        {
            if (_testAction == null) throw new InvalidOperationException("Test action not configured");
            await _testAction(doc, copiedSlideId);
        }
        finally
        {
            doc.AddAction(new DeleteSlideAction(copiedSlideId));
            await doc.ExecuteAsync();
        }
    }
}
