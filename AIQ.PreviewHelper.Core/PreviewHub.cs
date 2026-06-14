namespace AIQ.PreviewHelper.Core;

#if DEBUG
/// <summary>
/// Static service locator for debug-only preview functionality.
/// Accepts an <see cref="IPreviewSink"/> implementation via <see cref="Sink"/>
/// to avoid circular dependencies between domain projects and preview converters.
/// </summary>
public static class PreviewHub
{
    /// <summary>
    /// Stores logic for the conversion of any object into a previewable format.
    /// </summary>
    public static IPreviewSink? Sink { get; set; }

    /// <summary>
    /// Converts any object into a previewable format using logic present in <see cref="PreviewHub.Sink"/>
    /// if it was injected in at the start of the program.
    /// </summary>
    /// <param name="object">The object to preview.</param>
    public static void AddToPreviewDoc(this object @object)
    {
        Sink?.AddToPreviewDoc(@object);
    }

    /// <summary>
    /// Converts any object into a previewable format using logic present in <see cref="PreviewHub.Sink"/>
    /// if it was injected in at the start of the program and then previews it.
    /// </summary>
    /// <param name="object">The object to preview.</param>
    /// <param name="useWebView">If true, opens in a web-based viewer; otherwise uses the OS-default GLB viewer.</param>
    public static void PreviewViaHub(this object @object, bool useWebView = false)
    {
        Sink?.AddToPreviewDoc(@object);
        Sink?.PreviewDoc(useWebView);
    }
}
#endif