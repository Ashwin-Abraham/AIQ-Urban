using System.Drawing;
using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.Helpers;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ArchSitePlan.RhinoInterface.RhinoOSM;

public class RhinoOsm
{
    protected readonly RhinoDoc _doc;
    protected ObjectAttributes _attributes;
    protected GeometryBase? _geometry;

    public RhinoOsm(RhinoDoc doc)
    {
        _doc = doc;
    }

    protected void SetObjectAttributes(
        string layerName,
        Color color,
        long? osmId)
    {
        int layerIndex = LayerHelpers.CreateOrGetLayerIndex(_doc, layerName, color);
        _attributes = new ObjectAttributes() 
        { 
            LayerIndex = layerIndex
        };

        _attributes.SetUserString(
            OSMConstants.OsmIdAttributeKey, 
            osmId?.ToString()?? string.Empty);
    }

    public Guid AddToDocument()
    {
        if (_geometry == null) return Guid.Empty;

        var guid = _doc.Objects.Add(_geometry, _attributes);
        return guid;
    }
}
