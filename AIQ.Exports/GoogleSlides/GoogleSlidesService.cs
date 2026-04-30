using Google.Apis.Slides.v1;
using Google.Apis.Slides.v1.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using AIQ.Exports.GoogleSlides.Actions;

namespace AIQ.Exports.GoogleSlides;
public class GoogleSlidesService
{
    private readonly SlidesService _service;
    private readonly string _presentationId;

    public GoogleSlidesService(string serviceAccountJsonPath, string presentationId)
    {
        string json = File.ReadAllText(serviceAccountJsonPath);
        var credential = GoogleCredential.FromJson(json)
            .CreateScoped(Scopes);

        _service = new SlidesService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "AIQ.Exports"
        });

        _presentationId = presentationId;
    }

    private static string[] Scopes => ["https://www.googleapis.com/auth/presentations"];

    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var presentation = await _service.Presentations.Get(_presentationId).ExecuteAsync(cancellationToken);
            return presentation != null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IList<Page>> GetSlidesAsync()
    {
        var presentation = await _service.Presentations.Get(_presentationId).ExecuteAsync();
        return presentation.Slides ?? [];
    }

    public async Task<Page?> FindSlideByIndexAsync(int index)
    {
        var slides = await GetSlidesAsync();
        if (index < 0 || index >= slides.Count)
            return null;
        return slides[index];
    }

    public async Task<Page?> FindSlideByTitleAsync(string title)
    {
        var slides = await GetSlidesAsync();
        for (int i = 0; i < slides.Count; i++)
        {
            if (slides[i].ObjectId == title)
                return slides[i];
        }
        return null;
    }

    private static ReplaceAllTextRequest CreateReplaceAllTextRequest(
        string findText,
        string replaceText,
        IList<string>? pageObjectIds = null)
    {
        return new ReplaceAllTextRequest
        {
            ContainsText = new SubstringMatchCriteria
            {
                Text = findText,
                MatchCase = false
            },
            ReplaceText = replaceText,
            PageObjectIds = pageObjectIds
        };
    }

    private static BatchUpdatePresentationRequest CreateBatchUpdateRequest(Request request)
    {
        return new BatchUpdatePresentationRequest
        {
            Requests = [request]
        };
    }

    private async Task<int> ExecuteBatchUpdateAsync(BatchUpdatePresentationRequest batchRequest)
    {
        var response = await _service.Presentations.BatchUpdate(batchRequest, _presentationId).ExecuteAsync();
        return response.Replies?.Sum(r => r.ReplaceAllText?.OccurrencesChanged ?? 0) ?? 0;
    }

    public async Task<int> ReplaceTextAsync(string findText, string replaceText, string slideTitle)
    {
        var slide = await FindSlideByTitleAsync(slideTitle);
        List<string> pageObjectIds = [slide?.ObjectId];
        var request = CreateReplaceAllTextRequest(findText, replaceText, pageObjectIds);
        var batchRequest = CreateBatchUpdateRequest(new Request { ReplaceAllText = request });
        return await ExecuteBatchUpdateAsync(batchRequest);
    }

    public async Task<int> ReplaceTextAsync(string findText, string replaceText, int slideIndex)
    {
        var slide = await FindSlideByIndexAsync(slideIndex);
        List<string> pageObjectIds = [slide?.ObjectId];
        var request = CreateReplaceAllTextRequest(findText, replaceText, pageObjectIds);
        var batchRequest = CreateBatchUpdateRequest(new Request { ReplaceAllText = request });
        return await ExecuteBatchUpdateAsync(batchRequest);
    }

    public async Task<int> ReplaceTextAsync(string findText, string replaceText)
    {
        var request = CreateReplaceAllTextRequest(findText, replaceText);
        var batchRequest = CreateBatchUpdateRequest(new Request { ReplaceAllText = request });
        return await ExecuteBatchUpdateAsync(batchRequest);
    }

    public async Task<int> ExecuteBatchAsync(List<IGoogleSlidesAction> actions, CancellationToken cancellationToken = default)
    {
        if (actions.Count == 0) return 0;

        var requests = actions.Select(action => action.ToRequest(_presentationId)).ToList();
        var batchRequest = new BatchUpdatePresentationRequest { Requests = requests };

        var response = await _service.Presentations.BatchUpdate(batchRequest, _presentationId).ExecuteAsync(cancellationToken);

        return response.Replies?.Sum(r =>
            (r.ReplaceAllText?.OccurrencesChanged ?? 0) +
            (r.ReplaceAllShapesWithImage?.OccurrencesChanged ?? 0)) ?? 0;
    }
}