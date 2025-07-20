using ArchSitePlan.OSM.Models.OsmPolyline;
using ArchSitePlan.RhinoBhomInterop;
using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.Helpers;
using Rhino;
using Rhino.DocObjects;

namespace ArchSitePlan.RhinoInterface.RhinoOSM.Polylines;

public class TransitRhOsm : RhinoOsm
{
    public TransitRhOsm(TransitOsm osmInput, RhinoDoc doc)
        :base(doc)
    {
        _geometry = osmInput.Centerline.ToOSMRhino();
        SetObjectAttributes(osmInput);
    }

    // Get Object Attribute
    private void SetObjectAttributes(TransitOsm osmInput)
    {
        string width = osmInput.Width.HasValue ?
            osmInput.Width.Value.ToString()
            : "xx";

        var layerName =
            LayerConstants.TransitOsmLayer + LayerConstants._Sep_
            + osmInput.Type + LayerConstants._Sep_
            + osmInput.SubType + LayerConstants._Sep_
            + width;

        int layerIndex = LayerHelpers.CreateOrGetLayerIndex(_doc, layerName);
        _attributes = new ObjectAttributes() { LayerIndex = layerIndex };
        _attributes.SetUserString(
            OSMConstants.OsmIdAttributeKey, 
            osmInput.Id?.ToString()?? string.Empty);
    }
}
