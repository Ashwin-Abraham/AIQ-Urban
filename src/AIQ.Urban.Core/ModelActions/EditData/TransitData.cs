using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions.EditData;

public record TransitData : IModelEditData
{
    public Curve2D Centerline { get; }
    public double Width { get; }
    public TransitType TransitType { get; }

    public TransitData(Curve2D centerline, double width, TransitType transitType)
    {
        Centerline = centerline.NormalizeTransitCenterline();
        if (Centerline.Geometry.IsSelfIntersecting())
            throw new ArgumentException("Transit centerline must not self-intersect.", nameof(centerline));

        Width = width;
        TransitType = transitType;
    }

    public Transit ToTransit()
    {
        return new Transit(Centerline, Width, TransitType);
    }
}
