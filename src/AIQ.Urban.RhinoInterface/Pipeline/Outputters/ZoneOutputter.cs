using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Utils;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class ZoneOutputter : IUrbanModelRhinoOutputter
{
    public void Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements)
    {
        var zones = elements.OfType<Zone>().ToList();
        foreach (var zone in zones)
            RhinoOutputHelper.OutputGeometry(
                doc, 
                zone.Boundary,
                RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.ZoneLayer(zone.Name), ColorUtils.DefaultZoneColor),
                false);
    }
}
