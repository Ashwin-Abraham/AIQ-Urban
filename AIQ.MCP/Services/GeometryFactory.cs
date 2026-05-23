using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino.Geometry;

namespace AIQ.MCP.Services;

/// <summary>Converts structured coordinate arrays into Rhino geometry objects.
/// Pure computation — no RhinoDoc required.</summary>
public static class GeometryFactory
{
    /// <summary>Creates a planar Curve2D from an array of [x,y] coordinate pairs.</summary>
    /// <param name="coords">At least 2 coordinate pairs. The curve is non-closed.</param>
    /// <returns>A Curve2D wrapping a PolylineCurve in the XY plane.</returns>
    public static Curve2D CreateCenterline(double[][] coords)
    {
        ArgumentNullException.ThrowIfNull(coords);

        if (coords.Length < 2)
            throw new ArgumentException("Centerline requires at least 2 coordinate pairs.");

        var points = coords.Select(c => new Point3d(c[0], c[1], 0)).ToArray();
        var curve = new PolylineCurve(points);

        return new Curve2D(curve, Guid.NewGuid());
    }

    /// <summary>Creates a closed Polygon2D from an array of [x,y] coordinate pairs.</summary>
    /// <param name="coords">At least 3 coordinate pairs forming a closed ring.
    /// If the last point does not match the first, it is auto-closed.</param>
    /// <returns>A Polygon2D wrapping a closed PolylineCurve in the XY plane.</returns>
    public static Polygon2D CreateFootprint(double[][] coords)
    {
        ArgumentNullException.ThrowIfNull(coords);

        if (coords.Length < 3)
            throw new ArgumentException("Footprint requires at least 3 coordinate pairs.");

        var points = coords.Select(c => new Point3d(c[0], c[1], 0)).ToList();

        // Auto-close if the ring is not closed
        var first = points[0];
        var last = points[^1];
        if (first.DistanceTo(last) > 1e-6)
            points.Add(first);

        var curve = new PolylineCurve(points);

        if (!curve.IsClosed)
            throw new ArgumentException("Footprint must form a closed ring.");

        return new Polygon2D(curve);
    }

    /// <summary>Resolves a transit type name to its TransitType enum value. Case-insensitive.</summary>
    /// <param name="name">The transit type name (e.g. "Road", "Railway", "CyclePath").</param>
    /// <returns>The matching TransitType enum value.</returns>
    public static TransitType ResolveTransitType(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (Enum.TryParse(typeof(TransitType), name, ignoreCase: true, out var result))
            return (TransitType)result!;

        var valid = string.Join(", ", Enum.GetNames<TransitType>());
        throw new ArgumentException($"Unknown transit type '{name}'. Valid types: {valid}.");
    }

    /// <summary>Resolves a use name to a Use instance from DefaultUses. Case-insensitive.</summary>
    /// <param name="name">The use name (e.g. "Residential", "Commercial").</param>
    /// <returns>The matching Use instance.</returns>
    public static Use ResolveUse(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var match = DefaultUses.All.FirstOrDefault(
            u => string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
            return match;

        var valid = string.Join(", ", DefaultUses.Names);
        throw new ArgumentException($"Unknown use name '{name}'. Valid uses: {valid}.");
    }
}
