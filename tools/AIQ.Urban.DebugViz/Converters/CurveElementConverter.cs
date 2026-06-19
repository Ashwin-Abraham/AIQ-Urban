using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;

namespace AIQ.Urban.DebugViz.Converters;

public class CurveElementConverter : IGeometryPreviewConverter
{
    private readonly Curve2DConverter _curve2DConverter = new();

    public bool IsPreviewable(object @object) => @object is CurveElement;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is not CurveElement curveElement) return;

        var curve2d = new Curve2D(curveElement.Geometry, curveElement.Id);
        _curve2DConverter.AddToPreviewDoc(curve2d, doc, color);
    }
}
