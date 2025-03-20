using ArchSitePlan.OSM.Models.OsmPolyline;
using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.RhinoBHoMInterop;
using Rhino;

namespace ArchSitePlan.RhinoInterface.RhinoOSM;

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
