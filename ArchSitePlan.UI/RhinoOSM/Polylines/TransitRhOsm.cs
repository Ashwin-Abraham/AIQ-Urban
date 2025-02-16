using ArchSitePlan.OSM.Models.OsmPolyline;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.UI.RhinoOSM.Polylines;

public class TransitRhOsm : IRhinoOSM<TransitOsm>
{
    public Guid RhinoId { get; private set; } = Guid.Empty;

    readonly RhinoDoc _doc;
    readonly Curve _curve;
    readonly ObjectAttributes _attributes;

    public TransitRhOsm
        (TransitOsm osmInput, RhinoDoc doc)
    {
        _doc = doc;
        _curve = osmInput.Centerline.ToOSMRhino();
        _attributes = GetObjectAttributes(osmInput);
    }

    // Get Object Attribute
    private ObjectAttributes GetObjectAttributes(TransitOsm osmInput)
    {
        string width = osmInput.Width.HasValue ?
            osmInput.Width.Value.ToString()
            : "xx";

        var layerName =
            LayerConstants.TransitOsmLayer + LayerConstants._Sep_
            + osmInput.Type + LayerConstants._Sep_
            + osmInput.SubType + LayerConstants._Sep_
            + width;

        int layerIndex = LayerHelpers.CreateOrGetLayerIndex(_doc, layerName);
        return new ObjectAttributes() { LayerIndex = layerIndex };
    }

    public bool AddToDocument()
    {
        RhinoId = _doc.Objects.AddCurve(_curve, _attributes);
        return RhinoId != Guid.Empty;
    }


}
