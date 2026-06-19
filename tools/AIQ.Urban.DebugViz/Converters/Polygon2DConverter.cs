using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;

namespace AIQ.Urban.DebugViz.Converters;

public class Polygon2DConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Polygon2D;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is not Polygon2D polygon) return;

        var attributes = RhinoHelper.MakeAttributes(
            doc, PreviewConstants.DebugCurveLayer, PreviewConstants.DebugGeometryColor, color);

        doc.Objects.AddCurve(polygon.Geometry, attributes);
    }
}
