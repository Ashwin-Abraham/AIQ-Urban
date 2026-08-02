using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;

namespace AIQ.Urban.RhinoInterface.Utils;

internal static class LayerConstants
{
    public static string Root => "AIQ Urban";
    public static string Building => "AIQ Urban::Building";
    public static string BuildingEnvelope => "AIQ Urban::Building::Envelope";
    public static string BuildingFloorPlates => "AIQ Urban::Building::FloorPlates";
    public static string Parcel => "AIQ Urban::Parcel";
    public static string ParcelBoundary => "AIQ Urban::ParcelBoundary";
    public static string Vegetation => "AIQ Urban::Vegetation";
    public static string Topography => "AIQ Urban::Topography";
    public static string Transit => "AIQ Urban::Transit";
    public static string ZoneRoot => "AIQ Urban::Zone";
    
    public static string TransitOutline(TransitType type, double width)
    {
        return $"{Transit}::{type}::Outline::{width}";
    }

    public static string TransitCenterline(TransitType type, double width)
    {
          return $"{Transit}::{type}::Centerline::{width}";
    }

    public static string TransitSurface(TransitType type)
    {
        return $"{Transit}::{type}::Surface";
    }

    public static string BuildingFloorPlate(Use use)
    {
        return $"{BuildingFloorPlates}::{use.Name}";
    }

    public static string ParcelLayer(Use use)
    {
        return $"{Parcel}::{use.Name}";
    }

    public static string ZoneLayer(string name)
    {
        return $"{ZoneRoot}::{name}";
    }

}
