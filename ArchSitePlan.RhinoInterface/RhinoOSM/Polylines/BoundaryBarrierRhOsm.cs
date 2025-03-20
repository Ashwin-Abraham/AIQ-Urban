using ArchSitePlan.OSM.Models.OsmPolyline;
using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.Helpers;
using ArchSitePlan.RhinoInterface.RhinoBHoMInterop;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.RhinoInterface.RhinoOSM;

public class BoundaryBarrierRhOsm :RhinoOsm
{
    public BoundaryBarrierRhOsm(BoundaryBarrierOsm osmInput, RhinoDoc doc)
    :base(doc)
    {
        var layerName = LayerConstants.BoundaryOsmLayer +
            LayerConstants._Sep_ +
            osmInput.Type;

        _geometry = osmInput.Centerline.ToOSMRhino();
        SetObjectAttributes(layerName, osmInput.Color, osmInput.Id);
    }
}
