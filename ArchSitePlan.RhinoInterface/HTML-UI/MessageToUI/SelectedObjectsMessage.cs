using ArchSitePlan.Core.Models;
using ArchSitePlan.RhinoInterface.HTML_UI.Websockets;
using BH.Engine.Data;
using BH.Engine.Geometry;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
internal class SelectedObjectMetricsMessage:IMessageToUI
{
    private const string _multipleObjects = "Multiple";

    public string Type { get; } = nameof(SelectedObjectMetricsMessage);
    public int ItemCount { get; }
    public string SelectedObjectType { get; }
    public List<(string Title,string Data)> Metrics { get; } 
    private SelectedObjectMetricsMessage(
        string selectedObjectType,
        int itemCount,
        List<(string Title, string Data)> metrics)
    {
        SelectedObjectType = selectedObjectType;
        ItemCount = itemCount;
        Metrics = metrics;
    }

    public static SelectedObjectMetricsMessage? FromBuildings(List<Building> buildings)
    {
        if (buildings.Count == 0)
            return null;

        var bua = buildings.Sum(x => x.BUA()).Round(1);
        var uses = buildings.Select(x => x.GetLargestUse()?? AppLanduses.Undefined)
            .Distinct()
            .ToList();

        string use = _multipleObjects;
        if (uses.Count == 1)
            use = uses[0].Name;

        var footprintArea = buildings.Sum(x => x.FootprintArea()).Round(1);

        (string Title, string Data)[] items = [
            ("Use", use),
            ("Built Area", bua.ToString()),
            ("Footprint Area", footprintArea.ToString())
            ];

        return new SelectedObjectMetricsMessage("Building", buildings.Count, items.ToList());
    }

    public static SelectedObjectMetricsMessage? FromParcels(List<Parcel> parcels)
    {
        if (parcels.Count == 0)
            return null;

        var uses = parcels.Select(x => x.LandUse.Name).Distinct().ToList();
        string use = _multipleObjects;
        if (uses.Count == 1)
            use = uses[0];

        var bua = parcels.Sum(x => x.BUA()).Round(1);
        var siteArea = parcels.Sum(x => x.Area()).Round(1);

        var far = (bua / siteArea).Round(2);
        var perimeter = parcels.Sum(x => x.Perimeter()).Round(1);

        (string Title, string Data)[] items = [
            ("Use", use),
            ("Perimeter", perimeter.ToString()),
            ("Site Area", siteArea.ToString()),
            ("Built Area", bua.ToString()),
            ("FAR", far.ToString()),
           ];

        return new SelectedObjectMetricsMessage("Parcel", parcels.Count, items.ToList());
    }

    public static SelectedObjectMetricsMessage? FromRoads(List<Road> roads)
    {
        if (roads.Count == 0)
            return null;

        var widths = roads.Select(x => x.Width).Distinct().ToList();
        string width = _multipleObjects;
        if (widths.Count == 1)
            width = widths[0].ToString();

        var length = roads.Sum(x => x.Centerline.Length()).Round(1);

        (string Title, string Data)[] items = [
            ("Width", width),
            ("Length", length.ToString()),
           ];

        return new SelectedObjectMetricsMessage("Transit", roads.Count, items.ToList());
    }

    public void BroadcastMessage()
    =>   WebSocketComms.BroadcastMessage(this);

    public static SelectedObjectMetricsMessage FromEmpty()
    {
        return new("Empty",0, new());
    }

    public static SelectedObjectMetricsMessage FromDividers(List<ParcelDivider> dividers)
    {
        var length = dividers.Sum(x => x.Curve.ILength()).Round(1);

        (string Title, string Data)[] items = [
            ("Length", length.ToString()),
           ];

        return new SelectedObjectMetricsMessage("Divider", dividers.Count, items.ToList());
    }
}