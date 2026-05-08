using ArchSitePlan.OSM.Models.OsmPolygon;
using ArchSitePlan.RhinoBhomInterop;
using ArchSitePlan.RhinoInterface.Constants;
using Rhino;

namespace ArchSitePlan.RhinoInterface.RhinoOSM.Areas;

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
