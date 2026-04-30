using AIQ.Exports.GoogleSlides.Actions;

namespace AIQ.Exports.GoogleSlides;

public interface IGoogleSlidesDocument
{
    string PresentationId { get; }
    List<IGoogleSlidesAction> PendingActions { get; }
    Task<bool> ValidateAsync(CancellationToken cancellationToken = default);
    Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
    void AddAction(IGoogleSlidesAction action);
    void ClearActions();
}
