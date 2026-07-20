using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Utils;
using AIQ.Urban.RhinoInterface.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class TransitSurfaceOutputter(ILoggerFactory? _loggerFactory = null) : IUrbanModelRhinoOutputter
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<TransitSurfaceOutputter>();

    public void Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements)
    {
        foreach (var (profile, type) in model.TransitSurfaces)
            RhinoOutputHelper.OutputGeometry(doc, profile, RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.TransitSurface(type), ColorUtils.GetTransitTypeColor(type)), true);

        _logger.LogInformation("Output {Count} transit surfaces to Rhino", model.TransitSurfaces.Count);
    }
}
