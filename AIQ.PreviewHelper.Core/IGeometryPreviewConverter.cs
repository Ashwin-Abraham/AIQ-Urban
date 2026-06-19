using System.Drawing;
using Rhino;

namespace AIQ.PreviewHelper.Core;

public interface IGeometryPreviewConverter
{
    bool IsPreviewable(object @object);

    void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null);
}