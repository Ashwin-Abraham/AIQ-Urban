using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.DebugViz.Converters;

public class Polygon2DConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Polygon2D;

    public void AddToPreviewDoc(object @object, RhinoDoc doc)
    {
        if (@object is not Polygon2D polygon) return;

        var layer = RhinoHelper.EnsureLayer(
            doc, PreviewConstants.DebugCurveLayer, PreviewConstants.DebugGeometryColor);

        doc.Objects.AddCurve(polygon.Geometry, new ObjectAttributes { LayerIndex = layer });
    }
}
