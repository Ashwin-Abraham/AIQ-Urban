using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models;
using Rhino;

namespace AIQ.Urban.DebugViz.Converters;

public class ZoneConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Zone || @object is IEnumerable<Zone>;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is Zone zone)
            AddToPreviewDoc(zone, doc, color);

        if (@object is IEnumerable<Zone> zones)
            foreach (var z in zones)
                AddToPreviewDoc(z, doc, color);
    }

    private static void AddToPreviewDoc(Zone zone, RhinoDoc doc, Color? color)
    {
        var layerPath = $"{PreviewConstants.ZoneLayer}::{zone.Name}";
        var attributes = RhinoHelper.MakeAttributes(doc, layerPath, PreviewConstants.ZoneDefaultColor, color);

        doc.Objects.AddCurve(zone.Boundary.Geometry, attributes);
    }
}
