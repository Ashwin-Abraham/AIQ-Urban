using System.Drawing;
using AIQ.PreviewHelper.Core;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz.Converters;
public class DefaultCurveConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Curve || @object is IEnumerable<Curve>;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is Curve curve)
            AddToPreviewDoc(curve, doc, color);

        if (@object is IEnumerable<Curve> curves)
            foreach (var c in curves)
                AddToPreviewDoc(c, doc, color);
    }

    private static void AddToPreviewDoc(Curve curve, RhinoDoc doc, Color? color)
    {
        var attributes = RhinoHelper.MakeAttributes(
            doc, PreviewConstants.DebugCurveLayer, PreviewConstants.DebugGeometryColor, color);

        doc.Objects.AddCurve(curve, attributes);
    }
}
