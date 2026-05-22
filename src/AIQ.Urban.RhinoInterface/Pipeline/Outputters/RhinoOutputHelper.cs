using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.RhinoInterface.Extensions;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

internal static class RhinoOutputHelper
{
    internal static ObjectAttributes MakeAttributes(RhinoDoc doc, string layerPath)
    {
        var layerIndex = doc.Layers.EnsureLayerExists(layerPath).Index;
        return new ObjectAttributes { LayerIndex = layerIndex };
    }

    internal static void OutputGeometry(RhinoDoc doc, Curve2D geometry, ObjectAttributes attributes)
    {
        geometry.Id = doc.Objects.AddCurve(geometry.Geometry, attributes);
    }

    internal static void OutputGeometry(RhinoDoc doc, Profile2D geometry, ObjectAttributes attributes, bool asBrep)
    {
        if (asBrep)
        {
            geometry.Id = doc.Objects.AddBrep(geometry.Geometry, attributes);
        }
        else
        {
            OutputGeometry(doc, geometry.Outline, attributes, false);

            foreach (var hole in geometry.Holes)
                OutputGeometry(doc, hole, attributes, false);
        }
    }

    internal static void OutputGeometry(RhinoDoc doc, Polygon2D geometry, ObjectAttributes attributes, bool asBrep)
    {
        if(asBrep)
        {
            var brep = geometry.ToBrep();
            geometry.Id = doc.Objects.AddBrep(brep, attributes);
        }
        else
            geometry.Id = doc.Objects.AddCurve(geometry.Geometry, attributes);
    }
}
