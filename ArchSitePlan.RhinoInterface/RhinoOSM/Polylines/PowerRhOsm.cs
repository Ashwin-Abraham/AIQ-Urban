using ArchSitePlan.OSM.Models.OsmPolyline;
using ArchSitePlan.RhinoInterface.Constants;
using Rhino;

namespace ArchSitePlan.RhinoInterface.RhinoOSM;

public class PowerRhOsm : RhinoOsm
{
    public PowerRhOsm(PowerOsm osmInput, RhinoDoc doc):base(doc)
    {
        var layerName = LayerConstants.PowerOsmLayer +
            LayerConstants._Sep_ +
            osmInput.Type;

        SetObjectAttributes(layerName, osmInput.Color, osmInput.Id);
    }
}
