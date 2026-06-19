using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;

namespace AIQ.Urban.DebugViz.Converters;

public class Profile2DConverter : IGeometryPreviewConverter
{
    private readonly Polygon2DConverter _polygon2DConverter = new();

    public bool IsPreviewable(object @object) => @object is Profile2D;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is not Profile2D profile) return;

        profile.Holes.Append(profile.Outline)
            .ToList()
            .ForEach(x => _polygon2DConverter.AddToPreviewDoc(x, doc, color));
    }
}
