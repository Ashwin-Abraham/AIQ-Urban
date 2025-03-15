using ArchSitePlan.OSM.Models.OsmPolygon;
using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.Helpers;
using ArchSitePlan.RhinoInterface.RhinoBHoMInterop;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

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
