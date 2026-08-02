using System.Drawing;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class ParcelBoundaryOutputter(ILoggerFactory? _loggerFactory = null) : IUrbanModelRhinoOutputter
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance)
        .CreateLogger<ParcelBoundaryOutputter>();

    public void Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements)
    {
        var boundaries = elements.OfType<ParcelBoundary>().ToList();
        foreach (var boundary in boundaries)
        {
            RhinoOutputHelper.OutputGeometry(
                doc,
                boundary.Curve,
                RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.ParcelBoundary, Color.DarkViolet));
        }

        _logger.LogInformation("Output {Count} parcel boundaries to Rhino", boundaries.Count);
    }
}
