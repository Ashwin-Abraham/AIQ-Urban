using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;

namespace AIQ.Urban.DebugViz.Converters;

public class Profile2DConverter : IGeometryPreviewConverter
{
    private readonly PolyCurve2DConverter _polyCurve2DConverter = new();
    private readonly DefaultBrepConverter _brepElementConverter = new();

    public bool IsPreviewable(object @object) => @object is Profile2D || @object is IEnumerable<Profile2D>;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is Profile2D profile)
            AddToPreviewDoc(profile, doc, color);

        if (@object is IEnumerable<Profile2D> profiles)
            profiles.ToList().ForEach(x => AddToPreviewDoc(x, doc, color));
    }

    private void AddToPreviewDoc(Profile2D profile, RhinoDoc doc, Color? color)
    {
        _brepElementConverter.AddToPreviewDoc(profile.Geometry, doc, color);

        profile.Holes.Append(profile.Outline)
            .ToList()
            .ForEach(x => _polyCurve2DConverter.AddToPreviewDoc(x, doc, color));
    }
}
