using Rhino.Geometry;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.Models.UrbanElements;

/// <summary>A linear transportation corridor of a given <see cref="Enums.TransitType"/>.</summary>
public class Transit : IUrbanElement
{
    /// <summary>Path along the corridor (must be planar at a consistent Z).</summary>
    public CurveElement Centerline { get; }

    /// <summary>Closed boundary curve representing the full right-of-way.</summary>
    public CurveElement Outline { get; }

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
        CurveElement centerline,
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

    private static CurveElement CalculateOutline(Curve centerline, double width, double tolerance)
    {
        if (!centerline.TryGetPlane(out var plane))
            throw new InvalidOperationException("Centerline must be planar.");

        var outline = CalculateOffset(centerline, width, plane, tolerance);
        return new CurveElement(outline, Guid.NewGuid());
    }

    private static Curve CalculateOffset(Curve centerline, double width, Plane plane, double tolerance)
    {
        var halfWidth = width / 2.0;
        var offsetCurves1 = centerline.Offset(plane, halfWidth, tolerance, CurveOffsetCornerStyle.Sharp);
        if (offsetCurves1 is not { Length: > 0 })
            throw new InvalidOperationException("Failed to offset centerline positively.");

        var offsetCurves2 = centerline.Offset(plane, -halfWidth, tolerance, CurveOffsetCornerStyle.Sharp);
        if (offsetCurves2 is not { Length: > 0 })
            throw new InvalidOperationException("Failed to offset centerline negatively.");

        var offset1 = offsetCurves1[0];
        var offset2 = offsetCurves2[0];

        var startCap = new Line(offset1.PointAtStart, offset2.PointAtStart).ToNurbsCurve();
        var endCap = new Line(offset1.PointAtEnd, offset2.PointAtEnd).ToNurbsCurve();

        var offset2Reversed = offset2.DuplicateCurve();
        offset2Reversed.Reverse();

        var segments = new Curve[] { offset1, endCap, offset2Reversed, startCap };
        var joined = Curve.JoinCurves(segments, tolerance);

        if (joined is not { Length: > 0 })
            throw new InvalidOperationException("Failed to join transit outline curves.");

        return joined[0];
    }

    public List<Guid> GetGeometryIds()
    {
        return [Centerline.Id, Outline.Id];
    }
}
