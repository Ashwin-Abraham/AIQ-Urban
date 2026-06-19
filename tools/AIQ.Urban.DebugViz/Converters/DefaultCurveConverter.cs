using System.Drawing;
using AIQ.PreviewHelper.Core;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz.Converters;
public class DefaultCurveConverter : IGeometryPreviewConverter
{
    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is not Curve curve) return;

        var attributes = RhinoHelper.MakeAttributes(
            doc, PreviewConstants.DebugCurveLayer, PreviewConstants.DebugGeometryColor, color);

        doc.Objects.AddCurve(curve, attributes);
    }

    public bool IsPreviewable(object @object)
    {
        return @object is Curve;
    }
}
