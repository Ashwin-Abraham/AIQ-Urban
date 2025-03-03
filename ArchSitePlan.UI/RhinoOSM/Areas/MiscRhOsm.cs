using ArchSitePlan.OSM.Models.OsmPolygon;
using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Helpers;
using ArchSitePlan.UI.RhinoBHoMInterop;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.UI.RhinoOSM.Areas;

class MiscRhOsm : RhinoOsm
{
    public Guid RhinoId { get; private set; } = Guid.Empty;

    public MiscRhOsm(MiscOsm osmInput, RhinoDoc doc)
        :base(doc)
    {
        _geometry = osmInput.Boundary.ToOSMRhino();
        SetObjectAttributes(LayerConstants.MiscOsmLayer,
            osmInput.Color, 
            osmInput.Id);
    }
}
