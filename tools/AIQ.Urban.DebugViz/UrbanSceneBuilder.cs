using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.DebugViz.Converters;
using Rhino;

namespace AIQ.Urban.DebugViz;

public class UrbanSceneBuilder
{
    private readonly RhinoDoc _doc;
    private readonly BuildingConverter _buildingConverter = new();
    private readonly TransitConverter _transitConverter = new();
    private readonly TransitSurfacesConverter _transitSurfacesConverter = new();
    private readonly ParcelConverter _parcelConverter = new();
    private readonly ZoneConverter _zoneConverter = new();

    public UrbanSceneBuilder()
    {
        _doc = RhinoDoc.CreateHeadless(null);
    }

    public UrbanSceneBuilder AddUrbanElements(IEnumerable<IUrbanElement> elements)
    {
        foreach (var element in elements)
        {
            switch (element)
            {
                case Building building:
                    _buildingConverter.AddToPreviewDoc(building, _doc);
                    break;
                case Transit transit:
                    _transitConverter.AddToPreviewDoc(transit, _doc);
                    break;
                case Parcel parcel:
                    _parcelConverter.AddToPreviewDoc(parcel, _doc);
                    break;
                case Zone zone:
                    _zoneConverter.AddToPreviewDoc(zone, _doc);
                    break;
            }
        }
        return this;
    }

    public UrbanSceneBuilder AddTransitSurfaces(IEnumerable<(Profile2D Profile, TransitType Type)> surfaces)
    {
        _transitSurfacesConverter.AddToPreviewDoc(surfaces, _doc);
        return this;
    }

    public RhinoDoc Build() => _doc;
}
