using AIQ.Urban.Core;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Utils;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class ZoneOutputter(List<Zone> zones) : IUrbanModelRhinoOutputter
{
    public void Run(UrbanModel model, RhinoDoc doc)
    {
        foreach (var zone in zones)
            RhinoOutputHelper.OutputGeometry(
                doc, 
                zone.Boundary,
                RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.ZoneLayer(zone.Name), ColorUtils.DefaultZoneColor),
                false);
    }
}
