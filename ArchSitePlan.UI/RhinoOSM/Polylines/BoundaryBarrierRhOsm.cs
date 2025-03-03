using ArchSitePlan.OSM.Models.OsmPolyline;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.UI.RhinoOSM;

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
