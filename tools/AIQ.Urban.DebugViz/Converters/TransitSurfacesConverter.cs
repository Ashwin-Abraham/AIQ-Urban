using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;

namespace AIQ.Urban.DebugViz.Converters;

public class TransitSurfacesConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is IEnumerable<(Profile2D, TransitType)>;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is not IEnumerable<(Profile2D Profile, TransitType Type)> surfaces) return;

        foreach (var (profile, type) in surfaces)
        {
            var transitColor = ColorHelpers.ColorForTransitType(type);
            var effectiveColor = color ?? transitColor;
            var attributes = RhinoHelper.MakeAttributes(
                doc, PreviewConstants.TransitSurfaceLayer(type), transitColor, effectiveColor);

            profile.Holes.Append(profile.Outline)
                .ToList()
                .ForEach(x => doc.Objects.AddCurve(x.Geometry, attributes));
        }
    }
}
