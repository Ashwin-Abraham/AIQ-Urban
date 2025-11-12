using System.Drawing;

namespace ArchSitePlan.RhinoInterface.Constants;

public static class LayerConstants
{
    public const string MainModellingLayer = "AIQ Urban Model";
    public const string _Sep_ = "::";
    public static string StudyAreaLayer = $"{MainModellingLayer}{_Sep_}Study Area";
    public static string BuildingFootprintLayer = $"{BuildingLayer}{_Sep_}Footprint";
    public static string BuildingFloorOutlineLayer = $"{BuildingLayer}{_Sep_}Floor Plate Outline";
    public static string BuildingMassLayer = $"{BuildingLayer}{_Sep_}Mass";
    public const string BuildingLayer = $"{MainModellingLayer}{_Sep_}Buildings";
    public const string ParcelsLayer = $"{MainModellingLayer}{_Sep_}Parcels";
    public const string RoadLayer = $"{MainModellingLayer}{_Sep_}Roads";
    public const string RoadOutlineLayer = $"{RoadLayer}{_Sep_}Outline";
    public const string RoadCLLayer = $"{RoadLayer}{_Sep_}CL";
    public const string RoadROWLayer = $"{RoadLayer}{_Sep_}RoW";
    public const string ParcelDividerLayer = $"{MainModellingLayer}{_Sep_}ParcelDivider";
    public const int DefaultLayerIndex = -1;
    public static Color RoadColour = Color.Black;
    public static Color BuildingMassColour = Color.LightGray;
    public static Color BuildingFloorOutlineColour = Color.DarkGray;
    public static Color BuildingFootprintColour = Color.DarkRed;
    public static Color StudyAreaColour = Color.IndianRed;
    public static Color ParcelDividerColour = Color.DarkViolet;

    /////// OSM Layers ///////
    public const string MainOSMLayer = "AIQ Map Import";
    public const string ClosedOSMWays = MainOSMLayer + _Sep_ + "Closed Ways";
    public const string OpenOSMWays = MainOSMLayer + _Sep_ + "Open Ways";

    // Closed Ways
    public const string LanduseOsmLayer = ClosedOSMWays + _Sep_ + "Landuse";
    public const string BuildingOsmLayer = ClosedOSMWays + _Sep_ + "Buildings";
    public const string NaturalOsmClosedOsmLayer = ClosedOSMWays + _Sep_ + "Buildings";
    public const string MiscOsmLayer = ClosedOSMWays + _Sep_ + "Place";

    // Open Ways
    public const string BoundaryOsmLayer = OpenOSMWays + _Sep_ + "Boundary/Barrier";
    public const string NaturalOsmOpenLayer = OpenOSMWays + _Sep_ + "Natural";
    public const string PowerOsmLayer = OpenOSMWays + _Sep_ + "Power";
    public const string TransitOsmLayer = OpenOSMWays + _Sep_ + "Transit";
    public const string WaterwayOsmLayer = OpenOSMWays + _Sep_ + "Waterways";
}
