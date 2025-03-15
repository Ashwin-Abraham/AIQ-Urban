using ArchSitePlan.OSM.Models.OsmPolyline;
using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.RhinoBHoMInterop;
using Rhino;

namespace ArchSitePlan.RhinoInterface.RhinoOSM.Polylines;

public class WaterwayRhOsm :RhinoOsm
{
    public WaterwayRhOsm (WaterwayOsm osmInput, RhinoDoc doc)
        :base(doc)
    {
        _geometry = osmInput.Centerline.ToOSMRhino();

        var layerName = LayerConstants.WaterwayOsmLayer
            + LayerConstants._Sep_ 
            + osmInput.Type;

        SetObjectAttributes(layerName, osmInput.Color, osmInput.Id);
    }
}
