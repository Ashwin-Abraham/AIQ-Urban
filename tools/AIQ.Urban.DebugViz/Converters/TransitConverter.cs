using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino;

namespace AIQ.Urban.DebugViz.Converters;

public class TransitConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Transit || @object is IEnumerable<Transit>;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is Transit transit)
            AddToPreviewDoc(transit, doc, color);

        if (@object is IEnumerable<Transit> transits)
            foreach (var t in transits)
                AddToPreviewDoc(t, doc, color);
    }

    private static void AddToPreviewDoc(Transit transit, RhinoDoc doc, Color? color)
    {
        var transitColor = ColorHelpers.ColorForTransitType(transit.TransitType);
        var effectiveColor = color ?? transitColor;

        var clAttributes = RhinoHelper.MakeAttributes(
            doc, PreviewConstants.TransitCenterlineLayer(transit.TransitType), transitColor, effectiveColor);
        doc.Objects.AddCurve(transit.Centerline.Geometry, clAttributes);

        var outlineAttributes = RhinoHelper.MakeAttributes(
            doc, PreviewConstants.TransitOutlineLayer(transit.TransitType), transitColor, effectiveColor);

        transit.Outline.Holes.Append(transit.Outline.Outline)
            .ToList()
            .ForEach(x => doc.Objects.AddCurve(x.Geometry, outlineAttributes));
    }
}
