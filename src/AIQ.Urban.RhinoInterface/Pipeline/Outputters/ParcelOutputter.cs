using AIQ.Urban.Core;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.RhinoInterface.Utils;
using Rhino;

namespace AIQ.Urban.RhinoInterface.Pipeline.Outputters;

public class ParcelOutputter(List<Parcel> parcels) : IUrbanModelRhinoOutputter
{
    public void Run(UrbanModel model, RhinoDoc doc)
    {
        foreach (var parcel in parcels)
            RhinoOutputHelper.OutputGeometry(doc, parcel.Outline, RhinoOutputHelper.MakeAttributes(doc, LayerConstants.ParcelLayer(parcel.Use)));
    }
}
