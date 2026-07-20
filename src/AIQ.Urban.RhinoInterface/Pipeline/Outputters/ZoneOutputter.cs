using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Utils;
using AIQ.Urban.RhinoInterface.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class ZoneOutputter(ILoggerFactory? _loggerFactory = null) : IUrbanModelRhinoOutputter
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<ZoneOutputter>();

    public void Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements)
    {
        var zones = elements.OfType<Zone>().ToList();
        foreach (var zone in zones)
            RhinoOutputHelper.OutputGeometry(
                doc, 
                zone.Boundary,
                RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.ZoneLayer(zone.Name), ColorUtils.DefaultZoneColor),
                false);

        _logger.LogInformation("Output {Count} zones to Rhino", zones.Count);
    }
}
