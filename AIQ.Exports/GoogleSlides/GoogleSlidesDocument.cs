using AIQ.Exports.GoogleSlides.Actions;

namespace AIQ.Exports.GoogleSlides;
public class GoogleSlidesDocument(
    string presentationId,
    string serviceAccountJsonPath) : IGoogleSlidesDocument
{
    private readonly GoogleSlidesService _service = new(serviceAccountJsonPath, presentationId);

    public string PresentationId { get; } = presentationId;
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
        return await _service.ValidateAsync(cancellationToken);
    }

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var count = await _service.ExecuteBatchAsync(PendingActions, cancellationToken);
        ClearActions();
        return count;
    }

}
