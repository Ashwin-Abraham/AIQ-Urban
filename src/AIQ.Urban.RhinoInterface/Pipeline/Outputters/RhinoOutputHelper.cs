using System.Drawing;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.RhinoInterface.Extensions;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

internal static class RhinoOutputHelper
{
    internal static ObjectAttributes MakeLayerDrivenAttributes(
        RhinoDoc doc,
        string layerPath,
        Color layerColor)
    {
        var layerIndex = doc.Layers.EnsureLayerExists(layerPath).Index;
        var layer = doc.Layers[layerIndex];
        layer.Color = layerColor;

        return new ObjectAttributes { LayerIndex = layerIndex };
    }

    internal static Guid OutputGeometry(RhinoDoc doc, Curve2D geometry, ObjectAttributes attributes)
    {
        geometry.Id = doc.Objects.AddCurve(geometry.Geometry, attributes);
        return geometry.Id;
    }

    internal static IEnumerable<Guid> OutputGeometry(RhinoDoc doc, Profile2D geometry, ObjectAttributes attributes, bool asBrep)
    {
        if (asBrep)
        {
            geometry.Id = doc.Objects.AddBrep(geometry.Geometry, attributes);
            yield return geometry.Id;
        }
        else
        {
            yield return OutputGeometry(doc, geometry.Outline, attributes, false);

            foreach (var hole in geometry.Holes)
                yield return OutputGeometry(doc, hole, attributes, false);
        }
    }

    internal static Guid OutputGeometry(RhinoDoc doc, Polygon2D geometry, ObjectAttributes attributes, bool asBrep)
    {
        if(asBrep)
        {
            var brep = geometry.ToBrep();
            geometry.Id = doc.Objects.AddBrep(brep, attributes);
        }
        else
            geometry.Id = doc.Objects.AddCurve(geometry.Geometry, attributes);

        return geometry.Id;
    }
}
