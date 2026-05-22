using AIQ.Urban.Core;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class TransitSurfaceOutputter : IUrbanModelRhinoOutputter
{
    public void Run(UrbanModel model, RhinoDoc doc)
    {
        foreach (var (profile, type) in model.TransitSurfaces)
            RhinoOutputHelper.OutputGeometry(doc, profile, RhinoOutputHelper.MakeAttributes(doc, LayerConstants.TransitSurface(type)), true);
    }
}
