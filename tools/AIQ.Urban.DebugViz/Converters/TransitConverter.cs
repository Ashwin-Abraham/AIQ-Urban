using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.DebugViz.Converters;

public class TransitConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Transit;

    public void AddToPreviewDoc(object @object, RhinoDoc doc)
    {
        if (@object is not Transit transit) return;

        var color = ColorHelpers.ColorForTransitType(transit.TransitType);

        var clLayer = RhinoHelper.EnsureLayer(
            doc, PreviewConstants.TransitCenterlineLayer(transit.TransitType), color);
        var clAttribs = new ObjectAttributes { LayerIndex = clLayer };
        doc.Objects.AddCurve(transit.Centerline.Geometry, clAttribs);

        var outlineLayer = RhinoHelper.EnsureLayer(
            doc, PreviewConstants.TransitOutlineLayer(transit.TransitType), color);
        var outlineAttribs = new ObjectAttributes { LayerIndex = outlineLayer };

        transit.Outline.Holes.Append(transit.Outline.Outline)
            .ToList()
            .ForEach(x => doc.Objects.AddCurve(x.Geometry, outlineAttribs));
    }
}
