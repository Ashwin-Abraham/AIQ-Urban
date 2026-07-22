using System.Drawing;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.RhinoInterface.Extensions;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

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

    internal static void OutputGeometry(RhinoDoc doc, Curve2D geometry, ObjectAttributes attributes)
    {
        var curve = geometry.Geometry.Duplicate() as Curve;
        geometry.Id = doc.Objects.AddCurve(curve, attributes);
    }

    internal static void OutputGeometry(RhinoDoc doc, Profile2D geometry, ObjectAttributes attributes, bool asBrep)
    {
        if (asBrep)
        {
            var brep = geometry.Geometry.Duplicate() as Brep;
            geometry.Id = doc.Objects.AddBrep(brep, attributes);
        }
        else
        {
            OutputGeometry(doc, geometry.Outline, attributes, false);

            foreach (var hole in geometry.Holes)
                OutputGeometry(doc, hole, attributes, false);
        }
    }

    internal static void OutputGeometry(RhinoDoc doc, Polycurve2D geometry, ObjectAttributes attributes, bool asBrep)
    {
        if(asBrep)
        {
            var brep = geometry.ToBrep();
            geometry.Id = doc.Objects.AddBrep(brep, attributes);
        }
        else
            geometry.Id = doc.Objects.AddCurve(geometry.Geometry.Duplicate() as Curve, attributes);
    }
}
