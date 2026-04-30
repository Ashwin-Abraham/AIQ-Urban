using Google.Apis.Slides.v1;
using Google.Apis.Slides.v1.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using AIQ.Exports.GoogleSlides.Actions;

namespace AIQ.Exports.GoogleSlides;
public class GoogleSlidesDocument(
    string presentationId,
    string serviceAccountJsonPath) : IGoogleSlidesDocument
{
    public string PresentationId { get; } = presentationId;
    public string ServiceAccountJsonPath { get; } = serviceAccountJsonPath;
    public List<IGoogleSlidesAction> PendingActions { get; } = [];

    public void AddAction(IGoogleSlidesAction action)
    {
        PendingActions.Add(action);
    }

    public void ClearActions()
    {
        PendingActions.Clear();
    }

    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var service = CreateService();

            var presentation = await service.Presentations.Get(PresentationId).ExecuteAsync();

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

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var service = CreateService();
        
        var requests = PendingActions
            .Select(action => action.ToRequest(PresentationId))
            .ToList();

        var batchRequest = new BatchUpdatePresentationRequest
        {
            Requests = requests
        };

        var response = await service.Presentations.BatchUpdate(batchRequest, PresentationId).ExecuteAsync();
        
        var count = response.Replies?.Sum(r => 
            (r.ReplaceAllText?.OccurrencesChanged ?? 0) +
            (r.ReplaceAllShapesWithImage?.OccurrencesChanged ?? 0)) ?? 0;

        ClearActions();
        return count;
    }

    private SlidesService CreateService(int timeoutSec = 5)
    {
        string json = File.ReadAllText(ServiceAccountJsonPath);
        var credential = GoogleCredential
            .FromJson(json)
            .CreateScoped(Scopes);

        return new SlidesService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "AIQ.Exports",
            HttpClientTimeout = TimeSpan.FromSeconds(timeoutSec)
        });
    }

    private static string[] Scopes => ["https://www.googleapis.com/auth/presentations"];
}