using System.Drawing;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using Rhino;
using Rhino.Geometry;

namespace AIQ.Urban.DebugViz.Converters;

public class ParcelConverter : IGeometryPreviewConverter
{
    public bool IsPreviewable(object @object) => @object is Parcel || @object is IEnumerable<Parcel>;

    public void AddToPreviewDoc(object @object, RhinoDoc doc, Color? color = null)
    {
        if (@object is Parcel parcel)
            AddToPreviewDoc(parcel, doc, color);

        if (@object is IEnumerable<Parcel> parcels)
            foreach (var p in parcels)
                AddToPreviewDoc(p, doc, color);
    }

    private static void AddToPreviewDoc(Parcel parcel, RhinoDoc doc, Color? color)
    {
        var layerPath = $"{PreviewConstants.ParcelLayer}::{parcel.Use.Name}";
        var attributes = RhinoHelper.MakeAttributes(doc, layerPath, parcel.Use.Color, color);

        var breps = Brep.CreatePlanarBreps(parcel.Outline.Geometry, 0.001);
        if (breps is { Length: > 0 })
        {
            var meshes = Mesh.CreateFromBrep(breps[0], MeshingParameters.FastRenderMesh);
            if (meshes != null)
                foreach (var m in meshes)
                    doc.Objects.AddMesh(m, attributes);
        }
    }
}
