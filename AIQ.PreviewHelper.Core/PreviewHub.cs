using System.Drawing;

namespace AIQ.PreviewHelper.Core;

#if DEBUG
public static class PreviewHub
{
    public static IPreviewSink? Sink { get; set; }

    public static void AddToPreviewDoc(this object @object, Color? color = null)
    {
        Sink?.AddToPreviewDoc(@object, color);
    }

    public static void PreviewViaHub(this object @object, bool useWebView = false, Color? color = null)
    {
        Sink?.AddToPreviewDoc(@object, color);
        Sink?.PreviewDoc(useWebView);
    }

    public static void PreviewViaHub(bool useWebView = false)
    {
        Sink?.PreviewDoc(useWebView);
    }

    public static void ResetDoc()
    {
        Sink?.ResetDoc();
    }
}
#endif