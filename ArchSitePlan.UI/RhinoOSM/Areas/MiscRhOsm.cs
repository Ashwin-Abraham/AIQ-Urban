using ArchSitePlan.OSM.Models.OsmPolygon;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.UI.RhinoOSM.Areas;

class MiscRhOsm : IRhinoOSM<MiscOsm>
{
    public Guid RhinoId { get; private set; } = Guid.Empty;

    readonly RhinoDoc _doc;
    readonly Curve? _boundary;
    readonly ObjectAttributes _attributes;

    public MiscRhOsm(MiscOsm osmInput, RhinoDoc doc)
    {
        _doc = doc;
        _boundary = osmInput.Boundary.ToOSMRhino();
        _attributes = GetObjectAttributes(doc, osmInput);
    }

    public static ObjectAttributes GetObjectAttributes(RhinoDoc doc, MiscOsm osmInput)
    {
        var layerName = LayerConstants.MiscOsmLayer;
        int layerIndex = LayerHelpers.CreateOrGetLayerIndex(doc, layerName, osmInput.Color);
        return new ObjectAttributes() { LayerIndex = layerIndex };
    }

    public bool AddToDocument()
    {
        if (_boundary == null) return false;

        RhinoId = _doc.Objects.AddCurve(_boundary, _attributes);
        return RhinoId != Guid.Empty;
    }
}
