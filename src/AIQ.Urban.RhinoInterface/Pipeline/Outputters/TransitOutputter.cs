using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Utils;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class TransitOutputter(List<Transit> transits) : IUrbanModelRhinoOutputter
{
    public void Run(UrbanModel model, RhinoDoc doc)
    {
        foreach (var transit in transits)
        {
            var transitColor = ColorUtils.GetTransitTypeColor(transit.TransitType);

            RhinoOutputHelper.OutputGeometry(doc, transit.Outline, RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.TransitOutline(transit.TransitType, transit.Width), transitColor), false).ToList();
            RhinoOutputHelper.OutputGeometry(doc, transit.Centerline, RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.TransitCenterline(transit.TransitType, transit.Width), transitColor));
        }
    }
}
