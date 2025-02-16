using ArchSitePlan.OSM.Models.OsmPolygon;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using BH.Engine.Geometry;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.UI.RhinoOSM.Areas;

class LandParcelRhOsm : IRhinoOSM<LandParcelOsm>
{
    public Guid RhinoId { get; private set; } = Guid.Empty;

    readonly RhinoDoc _doc;
    readonly Brep? _parcel;
    readonly ObjectAttributes _attributes;

    public LandParcelRhOsm(
        LandParcelOsm osmInput,
        RhinoDoc doc)
    {
        _doc = doc;
        var footPrint = osmInput.Boundary.ToOSMRhino();
        _parcel = Brep.CreatePlanarBreps(footPrint, 1e-05).FirstOrDefault();
        _attributes = GetObjectAttributes(osmInput, doc);
    }

    public static ObjectAttributes GetObjectAttributes(LandParcelOsm osmInput, RhinoDoc doc)
    {
        var layerName = LayerConstants.LanduseOsmLayer + LayerConstants._Sep_ + osmInput.Type;
        int layerIndex = LayerHelpers.CreateOrGetLayerIndex(doc, layerName, osmInput.Color);
        return new ObjectAttributes() { LayerIndex = layerIndex };
    }

    public bool AddToDocument()
    {
        if (_parcel == null) return false;

        RhinoId = _doc.Objects.AddBrep(_parcel, _attributes);
        return RhinoId != Guid.Empty;
    }
}
