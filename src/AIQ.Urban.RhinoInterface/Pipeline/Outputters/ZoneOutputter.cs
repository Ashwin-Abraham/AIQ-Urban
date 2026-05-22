using AIQ.Urban.Core;
using AIQ.Urban.Core.Models;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class ZoneOutputter(List<Zone> zones) : IUrbanModelRhinoOutputter
{
    public void Run(UrbanModel model, RhinoDoc doc)
    {
        foreach (var zone in zones)
            RhinoOutputHelper.OutputGeometry(doc, zone.Boundary, RhinoOutputHelper.MakeAttributes(doc, LayerConstants.ZoneLayer(zone.Name)));
    }
}
