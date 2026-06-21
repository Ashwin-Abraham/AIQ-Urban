using System.Drawing;

namespace AIQ.PreviewHelper.Core;

public interface IPreviewSink
{
    void PreviewDoc(bool useWebViewer = false);

    void AddToPreviewDoc(object @object, Color? color = null);

    void ResetDoc();
}
