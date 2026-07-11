using System.Globalization;
using AIQ.Urban.Core.Enums;

namespace AIQ.Urban.RhinoInterface.Utils;

internal static class LayerNameParser
{
    /// <summary>
    /// Parses transit type and width from a centerline or outline layer path.
    /// Returns null if the path doesn't match the expected format.
    /// </summary>
    public static (TransitType Type, double Width)? ParseTransitLayer(string layerPath)
    {
        // Expected: AIQ Urban::Transit::{Type}::Centerline::{width} or ::Outline::{width}
        var parts = layerPath.Split("::");
        if (parts.Length != 5)
            return null;
        if (parts[0] != "AIQ Urban" || parts[1] != "Transit")
            return null;
        if (parts[3] != "Centerline" && parts[3] != "Outline")
            return null;

        if (!Enum.TryParse<TransitType>(parts[2], ignoreCase: true, out var transitType))
            return null;
        if (!double.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var width))
            return null;

        return (transitType, width);
    }

    /// <summary>
    /// Parses use name from a building floor plate layer path.
    /// Returns null if the path doesn't match.
    /// </summary>
    public static string? ParseFloorPlateUse(string layerPath)
    {
        const string prefix = "AIQ Urban::Building::FloorPlates::";
        if (!layerPath.StartsWith(prefix, StringComparison.Ordinal))
            return null;
        var useName = layerPath.Substring(prefix.Length);
        return string.IsNullOrEmpty(useName) ? null : useName;
    }

    /// <summary>
    /// Parses use name from a parcel layer path.
    /// Returns null if the path doesn't match.
    /// </summary>
    public static string? ParseParcelUse(string layerPath)
    {
        const string prefix = "AIQ Urban::Parcel::";
        if (!layerPath.StartsWith(prefix, StringComparison.Ordinal))
            return null;
        var useName = layerPath.Substring(prefix.Length);
        return string.IsNullOrEmpty(useName) ? null : useName;
    }

    /// <summary>
    /// Parses zone name from a zone layer path.
    /// Returns null if the path doesn't match.
    /// </summary>
    public static string? ParseZoneName(string layerPath)
    {
        const string prefix = "AIQ Urban::Zone::";
        if (!layerPath.StartsWith(prefix, StringComparison.Ordinal))
            return null;
        var name = layerPath.Substring(prefix.Length);
        return string.IsNullOrEmpty(name) ? null : name;
    }
}
