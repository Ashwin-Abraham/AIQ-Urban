namespace AIQ.PreviewHelper.Core;

#if DEBUG
/// <summary>
/// Contains a space for the injection of preview logic without causing a circular dependancy.
/// <see cref="PreviewHub.Sink"/> will host preview helper logic that converts any object
/// into a previewable format if the preview helper for that object is present in the sink.
/// 
/// This class acts as an orchestrator for the preview conversion logic of the <see cref="IPreviewSink"/>
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
    /// <param name="object">The object to preview</param>
    /// <param name="previewStyle">The preview style index of the preview helper. 
    /// This allows for the presetting of complex preview logic</param>
    public static void AddToPreviewDoc(this object @object)
    {
        Sink?.AddToPreviewDoc(@object);
    }

    /// <summary>
    /// Converts any object into a previewable format using logic present in <see cref="PreviewHub.Sink"/>
    /// if it was injected in at the start of the program and then previews it.
    /// </summary>
    /// <param name="object">The object to preview</param>
    /// <param name="previewStyle">The preview style index of the preview helper. 
    /// This allows for the presetting of complex preview logic</param>
    public static void PreviewViaHub(this object @object, bool useWebView = false)
    {
        Sink?.AddToPreviewDoc(@object);
        Sink?.PreviewDoc(useWebView);
    }
}
#endif