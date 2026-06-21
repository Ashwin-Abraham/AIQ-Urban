using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;

namespace AIQ.Urban.DebugViz.Converters;

public class Curve2DConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Curve2D || @object is IEnumerable<Curve2D>;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is Curve2D curve2d)
            AddToPreviewDoc(curve2d, doc, color);

        if (@object is IEnumerable<Curve2D> curves)
            foreach (var c in curves)
                AddToPreviewDoc(c, doc, color);
    }

    private static void AddToPreviewDoc(Curve2D curve2d, RhinoDoc doc, Color? color)
    {
        var attributes = RhinoHelper.MakeAttributes(
            doc, PreviewConstants.DebugCurveLayer, PreviewConstants.DebugGeometryColor, color);

        doc.Objects.AddCurve(curve2d.Geometry, attributes);
    }
}
