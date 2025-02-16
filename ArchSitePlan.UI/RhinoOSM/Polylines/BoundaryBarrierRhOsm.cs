using ArchSitePlan.OSM.Models.OsmPolyline;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.UI.RhinoOSM;

public class BoundaryBarrierRhOsm : IRhinoOSM<BoundaryBarrierOsm>
{
    public Guid RhinoId { get; private set; } = Guid.Empty;

    readonly RhinoDoc _doc;
    readonly Curve _curve;
    readonly ObjectAttributes _attributes;

    public BoundaryBarrierRhOsm(BoundaryBarrierOsm osmInput, RhinoDoc doc)
    {
        _doc = doc;
        _curve = osmInput.Centerline.ToOSMRhino();
        _attributes = GetObjectAttributes(osmInput);
    }

    // Get Object Attribute
    private ObjectAttributes GetObjectAttributes(BoundaryBarrierOsm osmInput)
    {
        var layerName = LayerConstants.BoundaryOsmLayer +
            LayerConstants._Sep_ +
            osmInput.Type;

        int layerIndex = LayerHelpers.CreateOrGetLayerIndex(_doc, layerName, osmInput.Color);
        return new ObjectAttributes() { LayerIndex = layerIndex };
    }

    public bool AddToDocument()
    {
        RhinoId = _doc.Objects.AddCurve(_curve, _attributes);
        return RhinoId != Guid.Empty;
    }


}
