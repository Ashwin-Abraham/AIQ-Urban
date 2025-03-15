using ArchSitePlan.OSM.Models.OsmPolygon;
using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.Helpers;
using ArchSitePlan.RhinoInterface.RhinoBHoMInterop;
using BH.Engine.Geometry;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.RhinoInterface.RhinoOSM.Areas;

class LandParcelRhOsm : RhinoOsm
{
    public LandParcelRhOsm(
        LandParcelOsm osmInput,
        RhinoDoc doc):base(doc)
    {
        var footPrint = osmInput.Boundary.ToOSMRhino();
        _geometry = Brep.CreatePlanarBreps(footPrint, 1e-05).FirstOrDefault();

        var layerName = LayerConstants.LanduseOsmLayer + LayerConstants._Sep_ + osmInput.Type;
        SetObjectAttributes(layerName, osmInput.Color, osmInput.Id);
    }
}
