using ArchSitePlan.OSM.Models.OsmPolyline;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino;

namespace ArchSitePlan.UI.RhinoOSM;

public class NaturalOpenRhOsm : RhinoOsm
{
    public NaturalOpenRhOsm(NaturalOpenOsm osmInput, RhinoDoc doc)
        :base(doc)
    {
        var layerName = LayerConstants.NaturalOsmOpenLayer +
            LayerConstants._Sep_ +
            osmInput.Type;

        _geometry = osmInput.Centerline.ToOSMRhino();
        SetObjectAttributes(layerName, osmInput.Color, osmInput.Id);
    }
}
