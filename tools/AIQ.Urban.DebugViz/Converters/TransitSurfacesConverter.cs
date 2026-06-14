using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.DebugViz.Converters;

public class TransitSurfacesConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is IEnumerable<(Profile2D, TransitType)>;

    public void AddToPreviewDoc(object @object, RhinoDoc doc)
    {
        if (@object is not IEnumerable<(Profile2D Profile, TransitType Type)> surfaces) return;

        foreach (var (profile, type) in surfaces)
        {
            var color = ColorHelpers.ColorForTransitType(type);
            var layer = RhinoHelper.EnsureLayer(
                doc, PreviewConstants.TransitSurfaceLayer(type), color);
            var attribs = new ObjectAttributes { LayerIndex = layer };

            profile.Holes.Append(profile.Outline)
                .ToList()
                .ForEach(x => doc.Objects.AddCurve(x.Geometry, attribs));
        }
    }
}
