using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record TransitData : ICreationData
{
    public Curve2D Centerline { get; }
    public double Width { get; }
    public TransitType TransitType { get; }

    public TransitData(Curve2D centerline, double width, TransitType transitType)
    {
        Centerline = centerline.NormalizeTransitCenterline();
        Width = width;
        TransitType = transitType;
    }

    public Transit ToTransit()
    {
        return new Transit(Centerline, Width, TransitType);
    }
}
