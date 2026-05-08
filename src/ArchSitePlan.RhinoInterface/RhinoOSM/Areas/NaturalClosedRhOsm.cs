using ArchSitePlan.OSM.Models.OsmPolygon;
using ArchSitePlan.RhinoBhomInterop;
using ArchSitePlan.RhinoInterface.Constants;
using Rhino;

namespace ArchSitePlan.RhinoInterface.RhinoOSM.Areas;

class NaturalClosedRhOsm : RhinoOsm
{
    public NaturalClosedRhOsm( NaturalClosedOsm osmInput, RhinoDoc doc) 
        :base(doc)
    {
        _geometry = osmInput.Boundary.ToOSMRhino();
        var layerName = LayerConstants.NaturalOsmClosedOsmLayer 
            + LayerConstants._Sep_
            + osmInput.Type;

        SetObjectAttributes(layerName, osmInput.Color, osmInput.Id);
    }
}
