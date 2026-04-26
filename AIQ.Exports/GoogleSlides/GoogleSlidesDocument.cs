using Google.Apis.Slides.v1;
using Google.Apis.Slides.v1.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;

namespace AIQ.Exports.GoogleSlides;
public class GoogleSlidesDocument(
    string presentationId,
    string serviceAccountJson)
{
    public string PresentationId { get; } = presentationId;
    public string ServiceAccountJson { get; } = serviceAccountJson;
    public List<IGoogleSlidesAction> PendingActions { get; } = [];

    public void AddAction(IGoogleSlidesAction action)
    {
        PendingActions.Add(action);
    }

    public void ClearActions()
    {
        PendingActions.Clear();
    }

    public async Task<int> ExecuteAsync()
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

    private SlidesService CreateService()
    {
        var credential = GoogleCredential.FromJson(ServiceAccountJson)
            .CreateScoped(Scopes);

        return new SlidesService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "AIQ.Exports"
        });
    }

    private static string[] Scopes => ["https://www.googleapis.com/auth/presentations"];
}