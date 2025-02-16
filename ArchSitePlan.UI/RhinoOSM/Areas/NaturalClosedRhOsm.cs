using ArchSitePlan.OSM.Models.OsmPolygon;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.UI.RhinoOSM.Areas;

class NaturalClosedRhOsm : IRhinoOSM<NaturalClosedOsm>
{
    public Guid RhinoId { get; private set; } = Guid.Empty;

    readonly RhinoDoc _doc;
    readonly Curve? _boundary;
    readonly ObjectAttributes _attributes;

    public NaturalClosedRhOsm(
        NaturalClosedOsm osmInput,
        RhinoDoc doc)
    {
        _doc = doc;
        _boundary = osmInput.Boundary.ToOSMRhino();
        _attributes = GetObjectAttributes(doc, osmInput);
    }

    public static ObjectAttributes GetObjectAttributes(RhinoDoc doc, NaturalClosedOsm osmInput)
    {
        var layerName = LayerConstants.NaturalOsmClosedOsmLayer + LayerConstants._Sep_ + osmInput.Type;
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
