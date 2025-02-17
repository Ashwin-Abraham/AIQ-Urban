using ArchSitePlan.OSM.Models.OsmPolyline;
using ArchSitePlan.UI.Constants;
using Rhino;

namespace ArchSitePlan.UI.RhinoOSM;

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
