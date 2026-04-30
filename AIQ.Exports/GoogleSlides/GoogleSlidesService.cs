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
    private const string _scope = "https://www.googleapis.com/auth/presentations";

    public GoogleSlidesService(string serviceAccountJsonPath, string presentationId)
    {
        string json = File.ReadAllText(serviceAccountJsonPath);
        var credential = GoogleCredential.FromJson(json).CreateScoped(_scope);

        _service = new SlidesService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "AIQ.Exports"
        });

        _presentationId = presentationId;
    }


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