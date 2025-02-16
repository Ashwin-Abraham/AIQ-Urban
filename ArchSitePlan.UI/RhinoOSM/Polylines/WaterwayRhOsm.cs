using ArchSitePlan.OSM.Models.OsmPolyline;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.UI.RhinoOSM.Polylines;

public class WaterwayRhOsm : IRhinoOSM<WaterwayOsm>
{
    public Guid RhinoId { get; private set; } = Guid.Empty;

    readonly RhinoDoc _doc;
    readonly Curve _curve;
    readonly ObjectAttributes _attributes;

    public WaterwayRhOsm (WaterwayOsm osmInput, RhinoDoc doc)
    {
        _doc = doc;
        _curve = osmInput.Centerline.ToOSMRhino();
        _attributes = GetObjectAttributes(osmInput);
    }

    // Get Object Attribute
    private ObjectAttributes GetObjectAttributes(WaterwayOsm osmInput)
    {
        var layerName = LayerConstants.WaterwayOsmLayer 
            + LayerConstants._Sep_ + osmInput.Type;

        int layerIndex = LayerHelpers.CreateOrGetLayerIndex(_doc, layerName);
        return new ObjectAttributes() { LayerIndex = layerIndex };
    }

    public bool AddToDocument()
    {
        RhinoId = _doc.Objects.AddCurve(_curve, _attributes);
        return RhinoId != Guid.Empty;
    }


}
