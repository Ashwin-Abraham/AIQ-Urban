using System.Diagnostics;
using System.Drawing;
using AIQ.PreviewHelper.Core;
using Rhino;

namespace AIQ.Urban.DebugViz;
public sealed class PreviewSink : IPreviewSink
{
    private readonly HashSet<IGeometryPreviewConverter> _elementConverters;
    private readonly RhinoDoc _doc;

    public PreviewSink(HashSet<IGeometryPreviewConverter> elementConverters)
    {
        _doc = RhinoDoc.CreateHeadless(null);
        _elementConverters = elementConverters;
    }

    public void AddToPreviewDoc(object @object, Color? color = null)
    {
        var found = false;

        foreach (var converter in _elementConverters)
        {
            if (!converter.IsPreviewable(@object))
                continue;

            converter.AddToPreviewDoc(@object, _doc, color);
            found = true;
        }

        if (!found)
            ReportFailedType(@object);
    }

    public void PreviewDoc(bool useWebViewer = false)
    {
        Visualizer.View(_doc, viewLocal: !useWebViewer);
    }

    private static void ReportFailedType(object @object)
    {
        var failedType = @object.GetType();
        var message = "No preview converter loaded for type";
        Trace.WriteLine(failedType, message);
    }
}