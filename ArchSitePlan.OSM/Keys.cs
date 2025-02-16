using System.Drawing;

namespace ArchSitePlan.OSM;

public static class Keys
{
    public const string Landuse = "landuse";
    public const string Highway = "highway";
    public const string Width = "width";
    public const string Building = "building";
    public const string Height = "building";
    public const string Levels = "building";
    public const string Natural = "natural";
    public const string Geological = "geological";
    public const string Water = "water";
    public const string Waterway = "waterway";
    public const string Place = "place";
    public const string Tourism = "tourism";
    public const string Amenity = "amenity";
    public const string Shop = "shop";
    public const string Healthcare = "healthcare";
    public const string Craft = "craft";
    public const string Military = "military";
    public const string Historic = "historic";
    public const string Office = "office";
    public const string Sport = "sport";
    public const string Telecom = "telecom";
    public const string Power = "power";
    public const string Boundary = "boundary";
    public const string Barrier = "barrier";
    public const string AerialWay = "aerialway";
    public const string Aeroway = "aeroway";
    public const string Railway = "railway";

    public static List<(string Tag, Color Color)> TransitKeys { get; } = new()
    {
        (Highway,  Colors.Roads),
        (AerialWay, Colors.Aerialway),
        (Aeroway, Colors.Aeroway),
        (Railway, Colors.Railway)
    };

    /// <summary>
    /// The value of highway tag will take on these colors
    /// </summary>
    public static List<(string Use, Color Color)> RoadTypesAndColors { get; } =
        new()
        {
            ("motorway", Colors.Roads),
            ("trunk", Colors.Roads),
            ("primary", Colors.Roads),
            ("secondary", Colors.Roads),
            ("tertiary", Colors.Roads),
            ("unclassified", Colors.Roads),
            ("residential", Colors.Roads),
            ("motorway_link", Colors.Roads),
            ("trunk_link", Colors.Roads),
            ("primary_link", Colors.Roads),
            ("secondary_link", Colors.Roads),
            ("tertiary_link", Colors.Roads),
            ("living_street", Colors.Paths),
            ("service", Colors.Paths),
            ("pedestrian", Colors.Paths),
            ("track", Colors.Paths),
            ("bus_guideway", Colors.Roads),
            ("escape", Colors.Roads),
            ("raceway", Colors.Roads),
            ("road", Colors.Roads),
            ("busway", Colors.Roads),
            ("footway", Colors.Paths),
            ("bridleway", Colors.Paths),
            ("steps", Colors.Paths),
            ("corridor", Colors.Paths),
            ("path", Colors.Paths),
            ("via_ferrata", Colors.Paths),
            ("cycleway", Colors.Cycleway),
        };

    public static List<string> BoundaryBarrier { get; } = new()
    {
        Boundary, Barrier
    };

    public static List<(string Tag, Color Color)> NaturalClosedTagsAndColors { get; } = new()
    {
        (Natural, Colors.Natural),
        (Geological, Colors.Geological),
        (Water, Colors.Water),
        (Waterway, Colors.WaterWay)
    };

    public static List<string> SupercedingBuildingUseTags { get; } = new()
    {
        Tourism, Amenity
    };

    public static List<(string Tag, Color Color)> SupercedingLanduseTagsAndColors { get; } = new()
    {
        (Amenity, Colors.Amenity),
        (Shop, Colors.Shop),
        (Healthcare, Colors.Healthcare),
        (Craft, Colors.Craft),
        (Military, Colors.Military),
        (Historic, Colors.Historic),
        (Office, Colors.Office),
        (Tourism, Colors.Tourism),
        (Sport, Colors.Sport),
        (Telecom, Colors.Telecom)
    };
}
