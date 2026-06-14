namespace AIQ.PreviewHelper.Core;

/// <summary>
/// Adapter that decouples preview logic from domain projects to avoid circular dependencies.
/// Domain projects reference this interface; the concrete implementation
/// (in AIQ.Urban.DebugViz) is injected at module-init time via <see cref="PreviewHub.Sink"/>.
/// </summary>
public interface IPreviewSink
{
    /// <summary>
    /// Opens the preview document in the configured viewer.
    /// </summary>
    /// <param name="useWebViewer">If true, opens in a web-based viewer (Babylon.js sandbox);
    /// otherwise opens the GLB with the OS-default viewer.</param>
    void PreviewDoc(bool useWebViewer = false);

    /// <summary>
    /// Converts the provided object into previewable geometry and adds it to the preview document.
    /// </summary>
    /// <param name="object">The object to convert to previewable elements.</param>
    void AddToPreviewDoc(object @object);
}
