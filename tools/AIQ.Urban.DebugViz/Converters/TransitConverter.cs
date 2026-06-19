using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino;

namespace AIQ.Urban.DebugViz.Converters;

public class TransitConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Transit;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is not Transit transit) return;

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
