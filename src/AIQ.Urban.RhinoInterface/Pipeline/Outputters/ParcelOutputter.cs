using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class ParcelOutputter(ILoggerFactory? _loggerFactory = null) : IUrbanModelRhinoOutputter
{
    private readonly ILogger _logger = (_loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<ParcelOutputter>();

    public void Run(UrbanModel model, RhinoDoc doc, List<IUrbanElement> elements)
    {
        var parcels = elements.OfType<Parcel>().ToList();
        foreach (var parcel in parcels)
            RhinoOutputHelper.OutputGeometry(
                doc, 
                parcel.Outline,
                RhinoOutputHelper.MakeLayerDrivenAttributes(doc, LayerConstants.ParcelLayer(parcel.Use), parcel.Use.Color),
                true);

        _logger.LogInformation("Output {Count} parcels to Rhino", parcels.Count);
    }
}
