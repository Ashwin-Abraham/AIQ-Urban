using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.DebugViz.Converters;

public class ZoneConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Zone;

    public void AddToPreviewDoc(object @object, RhinoDoc doc)
    {
        if (@object is not Zone zone) return;

        var layer = RhinoHelper.EnsureLayer(
            doc, $"{PreviewConstants.ZoneLayer}::{zone.Name}", PreviewConstants.ZoneDefaultColor);
        var attribs = new ObjectAttributes { LayerIndex = layer };

        doc.Objects.AddCurve(zone.Boundary.Geometry, attribs);
    }
}
