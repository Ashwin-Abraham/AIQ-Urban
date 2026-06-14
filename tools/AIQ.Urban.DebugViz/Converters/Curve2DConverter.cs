using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.DebugViz.Converters;

public class Curve2DConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Curve2D;

    public void AddToPreviewDoc(object @object, RhinoDoc doc)
    {
        if (@object is not Curve2D curve2d) return;

        var layer = RhinoHelper.EnsureLayer(
            doc, PreviewConstants.DebugCurveLayer, PreviewConstants.DebugGeometryColor);

        doc.Objects.AddCurve(curve2d.Geometry, new ObjectAttributes { LayerIndex = layer });
    }
}
