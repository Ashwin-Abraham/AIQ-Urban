using System.Drawing;
using Rhino.Geometry;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanGraph;

namespace AIQ.Urban.Core.Models.UrbanElements;

/// <summary>A linear transportation corridor of a given <see cref="Enums.TransitType"/>.</summary>
public class Transit : IUrbanElement, IGraphableAsEdge
{
    /// <summary>Path along the corridor (planar at consistent Z).</summary>
    public Curve2D Centerline { get; set; }

    /// <inheritdoc/>
    public Curve GraphableGeometry => Centerline.Geometry;

    /// <summary>Closed boundary curve representing the full right-of-way.</summary>
    public Profile2D Outline { get; set; }

    /// <summary>Right-of-way width.</summary>
    public double Width { get; }

    /// <summary>Type of transit corridor.</summary>
    public TransitType TransitType { get; }

    /// <summary>Computed colour derived from transit type and comparative width. Set by TransitOutputter.</summary>
    public Color? Color { get; set; }

    public Guid Id => Centerline.Id;

    /// <param name="centerline">Planar path on the World XY plane.</param>
    /// <param name="width">Right-of-way width.</param>
    /// <param name="transitType">Type of corridor.</param>
    /// <param name="tolerance">Fitting tolerance for geometry operations.</param>
    public Transit(
        Curve2D centerline,
        double width,
        TransitType transitType,
        double tolerance = 0.001)
    {
        Centerline = centerline;
        Width = width;
        TransitType = transitType;
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

        var perimeter = new PolyCurve2D(outlines[0]);
        var holes = outlines.Except([outlines[0]])
            .Select(x => new PolyCurve2D(x))
            .ToList();

        return new Profile2D(perimeter, holes);
    }

    public Point3d GetRepresentativeCenter()
    {
        return Centerline.Geometry.PointAtNormalizedLength(0.5);
    }

    public List<Guid> GetGeometryIds()
    {
        List<Guid> ids = [Centerline.Id, Outline.Outline.Id];
        ids.AddRange(Outline.Holes.Select(x => x.Id));
        return ids;
    }
}
