using BH.Engine.Geometry;
using BH.oM.Geometry;
using CoordinateSharp;
using OsmSharp;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace ArchSitePlan.OSM.Extensions;

public static class WayExtensions
{
    public static bool HasLanduse(this OsmGeo geo)
    {
        string use = string.Empty;
        bool exists = geo.Tags?.TryGetValue(Keys.Landuse, out use)?? false;
        return exists && use != string.Empty;
    }

    public static List<Point> ToBHoMPoints(this Way way, List<Node> nodes)
    {
        var wayIds = way.Nodes.ToList();

        var filteredNodeLookup = nodes
            .Where(node => wayIds.Contains(node.Id ?? 0))
            .ToLookup(node => node.Id);

        var orderedNodes = way.Nodes
            .SelectMany(x => filteredNodeLookup[x])
            .ToList();

        var points = orderedNodes
            .Select(x => ToMeterPointProjNet(x))
            //.Where(x => x.Latitude != null && x.Longitude != null)
            //.Select(x => Create.Point(x.Latitude!.Value, x.Longitude!.Value))
            .ToList();

        return points;
    }

    private static Point? ToMeterPointProjNet(this Node node)
    {
        // Define WGS84 geographic coordinate system
        var wgs84 = GeographicCoordinateSystem.WGS84;

        // Compute UTM zone from longitude
        int utmZone = (int)Math.Floor((node.Longitude.Value + 180) / 6) + 1;
        bool isNorthernHemisphere = node.Latitude.Value >= 0;

        // Create UTM projected coordinate system based on computed zone and hemisphere
        var utm = ProjectedCoordinateSystem.WGS84_UTM(utmZone, isNorthernHemisphere);

        // Create a transformation from WGS84 to UTM
        var transformFactory = new CoordinateTransformationFactory();
        var transformation = transformFactory.CreateFromCoordinateSystems(wgs84, utm);

        // PROJ.4 expects (longitude, latitude) order
        double[] fromPoint = new double[] { node.Longitude.Value, node.Latitude.Value };
        double[] toPoint = transformation.MathTransform.Transform(fromPoint);

        // toPoint[0] is Easting (X) and toPoint[1] is Northing (Y).
        return Create.Point(toPoint[0], toPoint[1], 0);
    }

    private static Point? ToMeterPointPackage(this Node node)
    {
        // Create a Coordinate instance from WGS84 lat/lon.
        Coordinate coordinate = new Coordinate(node.Latitude.Value, node.Longitude.Value);

        // CoordinateSharp converts to UTM automatically.
        // The UTM property contains Easting and Northing in meters.
        double x = coordinate.UTM.Easting;
        double y = coordinate.UTM.Northing;

        // Create a Rhino Point3d in the XY plane (Z is set to 0).
        return Create.Point(x, y);
    }

    private static Point? ToMeterPoint(Node node)
    {
        if (!node.Latitude.HasValue || !node.Longitude.HasValue) return null;

        // Convert latitude from degrees to radians
        double latRad = node.Latitude.Value * (Math.PI / 180.0);
        double lonRad = node.Longitude.Value * (Math.PI / 180.0);

        // WGS84 constants
        const double equatorialRadius = 6378137.0; // in meters
        const double polarRadius = 6356752.3; // in meters

        double x = 111.32 * 1000 * node.Latitude.Value;

        double lengthPerLongt = 40075 * 1000 * Math.Cos(node.Latitude.Value) / 360;
        double y = node.Longitude.Value * lengthPerLongt;

        return Create.Point(x, y);
    }

    public static bool IsClosed(this Way way)
    {
        return way.Nodes.Length > 1 &&
            way.Nodes[0] == way.Nodes[^1];
    }
}
