using AIQ.Exports.GoogleSlides.Actions;
using Google.Apis.Http;
using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.GoogleSlides;
public class GoogleSlidesDocument(
    string presentationId,
    IConfigurableHttpClientInitializer credential) : IGoogleSlidesDocument
{
    private readonly GoogleSlidesService _service = new(credential, presentationId);

    public string PresentationId { get; } = presentationId;
    public List<IGoogleSlidesAction> PendingActions { get; } = [];

    public void AddAction(params IGoogleSlidesAction[] action)
    {
        PendingActions.AddRange(action);
    }

    void IGoogleSlidesDocument.AddAction(IGoogleSlidesAction action) => AddAction(action);

    public void ClearActions()
    {
        PendingActions.Clear();
    }

    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        return await _service.ValidateAsync(cancellationToken);
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var count = await _service.ExecuteBatchAsync(PendingActions, cancellationToken);
        ClearActions();
        return count;
    }

    public async Task<BatchUpdatePresentationResponse> ExecuteWithResponseAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _service.ExecuteBatchWithResponseAsync(PendingActions, cancellationToken);
        ClearActions();
        return response;
    }

    public async Task<Presentation> GetPresentationAsync(CancellationToken cancellationToken = default)
    {
        return await _service.GetPresentationAsync(cancellationToken);
    }

    public async Task<GoogleSlidesDocument> CopyPresentationAsync(string? name = null, CancellationToken cancellationToken = default)
    {
        var newId = await _service.CopyPresentationAsync(name, cancellationToken);
        return new GoogleSlidesDocument(newId, credential);
    }

    public GoogleSlidesDocument Clone()
    {
        var doc = new GoogleSlidesDocument(PresentationId, credential);
        doc.PendingActions.AddRange(PendingActions);

        return doc;
    }

}
