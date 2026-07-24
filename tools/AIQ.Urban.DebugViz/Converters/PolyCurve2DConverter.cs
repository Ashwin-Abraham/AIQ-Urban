using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;

namespace AIQ.Urban.DebugViz.Converters;

public class PolyCurve2DConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is PolyCurve2D || @object is IEnumerable<PolyCurve2D>;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is PolyCurve2D polyCurve)
            AddToPreviewDoc(polyCurve, doc, color);

        if (@object is IEnumerable<PolyCurve2D> polyCurves)
            foreach (var curve in polyCurves)
                AddToPreviewDoc(curve, doc, color);
    }

    private static void AddToPreviewDoc(PolyCurve2D polyCurve, RhinoDoc doc, Color? color)
    {
        var attributes = RhinoHelper.MakeAttributes(
            doc, PreviewConstants.DebugCurveLayer, PreviewConstants.DebugGeometryColor, color);

        doc.Objects.AddCurve(polyCurve.Geometry, attributes);
    }
}
