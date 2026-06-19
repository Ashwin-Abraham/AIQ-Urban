using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models;
using Rhino;

namespace AIQ.Urban.DebugViz.Converters;

public class ZoneConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Zone;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is not Zone zone) return;

        var layerPath = $"{PreviewConstants.ZoneLayer}::{zone.Name}";
        var attributes = RhinoHelper.MakeAttributes(doc, layerPath, PreviewConstants.ZoneDefaultColor, color);

        doc.Objects.AddCurve(zone.Boundary.Geometry, attributes);
    }
}
