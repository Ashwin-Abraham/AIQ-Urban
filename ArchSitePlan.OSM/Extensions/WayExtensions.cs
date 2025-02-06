using OsmSharp;
using OsmSharp.Tags;

namespace ArchSitePlan.OSM.Extensions;

public static class WayExtensions
{
    public static bool HasLanduse(this OsmGeo geo)
    {
        string use = string.Empty;
        bool exists = geo.Tags?.TryGetValue(Keys.Landuse, out use)?? false;
        return exists && use != string.Empty;
    }
}
