using ArchSitePlan.OSM.Models.OsmPolygon;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.UI.RhinoOSM.Areas;

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
