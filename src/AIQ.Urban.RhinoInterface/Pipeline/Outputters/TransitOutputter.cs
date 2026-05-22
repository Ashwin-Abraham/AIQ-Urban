using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class TransitOutputter(List<Transit> transits) : IUrbanModelRhinoOutputter
{
    public void Run(UrbanModel model, RhinoDoc doc)
    {
        foreach (var transit in transits)
        {
            RhinoOutputHelper.OutputGeometry(doc, transit.Outline, RhinoOutputHelper.MakeAttributes(doc, LayerConstants.TransitOutline(transit.TransitType, transit.Width)), false);
            RhinoOutputHelper.OutputGeometry(doc, transit.Centerline, RhinoOutputHelper.MakeAttributes(doc, LayerConstants.TransitCenterline(transit.TransitType, transit.Width)));
        }
    }
}
