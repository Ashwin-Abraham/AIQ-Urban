using System.Drawing;
using AIQ.PreviewHelper.Core;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz.Converters;
public class DefaultCurveConverter : IGeometryPreviewConverter
{
    public void AddToPreviewDoc(object @object, RhinoDoc doc)
    {
        var layer = RhinoHelper.EnsureLayer(
            doc, 
            PreviewConstants.DefaultCurveLayer,
            Color.FromArgb(15, Color.Gray));

        var attributes = new ObjectAttributes { LayerIndex = layer };

        if(@object is Curve curve) 
            doc.Objects.AddCurve(curve, attributes);
    }

    public bool IsPreviewable(object @object)
    {
        return @object is Curve;
    }
}
