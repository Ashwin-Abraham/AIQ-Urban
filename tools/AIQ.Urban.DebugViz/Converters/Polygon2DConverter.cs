using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;

namespace AIQ.Urban.DebugViz.Converters;

public class Polygon2DConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Polygon2D || @object is IEnumerable<Polygon2D>;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is Polygon2D polygon)
            AddToPreviewDoc(polygon, doc, color);

        if (@object is IEnumerable<Polygon2D> polygons)
            foreach (var p in polygons)
                AddToPreviewDoc(p, doc, color);
    }

    private static void AddToPreviewDoc(Polygon2D polygon, RhinoDoc doc, Color? color)
    {
        var attributes = RhinoHelper.MakeAttributes(
            doc, PreviewConstants.DebugCurveLayer, PreviewConstants.DebugGeometryColor, color);

        doc.Objects.AddCurve(polygon.Geometry, attributes);
    }
}
