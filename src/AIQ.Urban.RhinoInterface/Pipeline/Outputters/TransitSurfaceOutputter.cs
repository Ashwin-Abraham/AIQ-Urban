using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Utils;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class TransitSurfaceOutputter : IUrbanModelRhinoOutputter
{
    public void Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements)
    {
        foreach (var (profile, type) in model.TransitSurfaces)
            RhinoOutputHelper.OutputGeometry(doc, profile, RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.TransitSurface(type), ColorUtils.GetTransitTypeColor(type)), true);
    }
}
