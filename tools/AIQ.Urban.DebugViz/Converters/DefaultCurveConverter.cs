using System.Drawing;
using AIQ.PreviewHelper.Core;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz.Converters;
public class DefaultCurveConverter : IGeometryPreviewConverter
{
    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        var layer = RhinoHelper.EnsureLayer(
            doc, 
            PreviewConstants.DebugCurveLayer,
            PreviewConstants.DebugGeometryColor);

        var attributes = new ObjectAttributes 
        { 
            LayerIndex = layer,
        };

        if(color != null)
        {
            attributes.ColorSource = ObjectColorSource.ColorFromObject;
            attributes.ObjectColor = color.Value;
        }

        if(@object is Curve curve) 
            doc.Objects.AddCurve(curve, attributes);
    }

    public bool IsPreviewable(object @object)
    {
        return @object is Curve;
    }
}
