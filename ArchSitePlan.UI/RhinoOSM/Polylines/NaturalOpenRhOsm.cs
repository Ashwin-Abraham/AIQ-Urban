using ArchSitePlan.OSM.Models.OsmPolyline;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.UI.RhinoOSM;

public class NaturalOpenRhOsm : IRhinoOSM<PowerOsm>
{
    public Guid RhinoId { get; private set; } = Guid.Empty;

    readonly RhinoDoc _doc;
    readonly Curve _curve;
    readonly ObjectAttributes _attributes;

    public NaturalOpenRhOsm(NaturalOpenOsm osmInput, RhinoDoc doc)
    {
        _doc = doc;
        _curve = osmInput.Centerline.ToOSMRhino();
        _attributes = GetObjectAttributes(osmInput);
    }

    // Get Object Attribute
    private ObjectAttributes GetObjectAttributes(NaturalOpenOsm natural)
    {
        var layerName = LayerConstants.NaturalOsmOpenLayer +
            LayerConstants._Sep_ +
            natural.Type;

        int layerIndex = LayerHelpers.CreateOrGetLayerIndex(_doc, layerName, natural.Color);
        return new ObjectAttributes() { LayerIndex = layerIndex };
    }

    public bool AddToDocument()
    {
        RhinoId = _doc.Objects.AddCurve(_curve, _attributes);
        return RhinoId != Guid.Empty;
    }


}
