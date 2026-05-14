using Rhino.Geometry;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.Models.UrbanElements;

/// <summary>A linear transportation corridor of a given <see cref="Enums.TransitType"/>.</summary>
public class Transit : IUrbanElement
{
    /// <summary>Path along the corridor (planar at consistent Z).</summary>
    public Curve2D Centerline { get; }

    /// <summary>Closed boundary curve representing the full right-of-way.</summary>
    public Profile2D Outline { get; }

    /// <summary>Right-of-way width.</summary>
    public double Width { get; }

    /// <summary>Type of transit corridor.</summary>
    public TransitType TransitType { get; }

    /// <summary>Whether the corridor sits on ground level.</summary>
    public bool IsOnGround { get; }

    /// <summary>Ordered junction nodes along the corridor.</summary>
    public List<TransitNode> OrderedNodes { get; }

    /// <param name="centerline">Planar path at consistent Z.</param>
    /// <param name="width">Right-of-way width.</param>
    /// <param name="transitType">Type of corridor.</param>
    /// <param name="isOnGround">True if ground-level.</param>
    /// <param name="tolerance">Fitting tolerance for geometry operations.</param>
    public Transit(
        Curve2D centerline,
        double width,
        TransitType transitType,
        bool isOnGround = true,
        double tolerance = 0.001)
    {
        Centerline = centerline;
        Width = width;
        TransitType = transitType;
        IsOnGround = isOnGround;
        OrderedNodes = [];
        Outline = CalculateOutline(centerline.Geometry, width, tolerance);
    }

    private static Profile2D CalculateOutline(Curve centerline, double width, double tolerance)
    {
        if (!centerline.TryGetPlane(out var plane))
            throw new InvalidOperationException("Centerline must be planar.");

        var outlines = centerline.OffsetBothWays(width, plane, tolerance);

        return ToProfile2d(outlines);
    }

    private static Profile2D ToProfile2d(List<Curve> outlines)
    {
        outlines = outlines.OrderByDescending(x => x.GetLength()).ToList();

        var perimeter = new Polygon2D(outlines[0]);
        var holes = outlines.Except([outlines[0]])
            .Select(x => new Polygon2D(x))
            .ToList();

        return Profile2D.Create(perimeter, holes);
    }

    public Point3d GetRepresentativeCenter()
    {
        return Centerline.Geometry.PointAtNormalizedLength(0.5);
    }

    public List<Guid> GetGeometryIds()
    {
        return [Centerline.Id, Outline.Id];
    }
}
