using System.Drawing;
using AIQ.Primitives.Extensions;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Utils;
using AIQ.Urban.RhinoInterface.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class TransitOutputter(ILoggerFactory? _loggerFactory = null) : IUrbanModelRhinoOutputter
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<TransitOutputter>();

    private static readonly Dictionary<TransitType, (int Hue, double SatMin, double SatMax, double LightMin, double LightMax)> ColorConfig = new()
    {
        [TransitType.Road] = (0, 0, 0, 45, 85),
        [TransitType.Railway] = (25, 55, 95, 20, 45),
        [TransitType.CyclePath] = (120, 40, 85, 20, 50),
        [TransitType.Pedestrian] = (43, 55, 95, 30, 60),
        [TransitType.Waterway] = (219, 55, 95, 45, 75),
    };

    public void Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements)
    {
        var transits = elements.OfType<Transit>().ToList();
        SetTransitColors(transits);

        foreach (var transit in transits)
        {
            var transitColor = transit.Color ?? ColorUtils.GetTransitTypeColor(transit.TransitType);

            RhinoOutputHelper.OutputGeometry(doc, transit.Outline, RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.TransitOutline(transit.TransitType, transit.Width), transitColor), false);
            RhinoOutputHelper.OutputGeometry(doc, transit.Centerline, RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.TransitCenterline(transit.TransitType, transit.Width), transitColor));
        }

        _logger.LogInformation("Output {Count} transits to Rhino", transits.Count);
    }

    private static void SetTransitColors(List<Transit> transits)
    {
        var widthRanges = transits
            .GroupBy(t => t.TransitType)
            .ToDictionary(g => g.Key, g => (Min: g.Min(t => t.Width), Max: g.Max(t => t.Width)));

        foreach (var transit in transits)
        {
            if (!ColorConfig.TryGetValue(transit.TransitType, out var cfg))
                continue;

            var range = widthRanges.GetValueOrDefault(transit.TransitType);
            var t = range.Max == range.Min ? 0.5
                : Math.Clamp((transit.Width - range.Min) / (range.Max - range.Min), 0, 1);

            var s = cfg.SatMin + t * (cfg.SatMax - cfg.SatMin);
            var l = cfg.LightMax - t * (cfg.LightMax - cfg.LightMin);

            transit.Color = ColorExtensions.FromHsl(cfg.Hue, s, l);
        }
    }
}
