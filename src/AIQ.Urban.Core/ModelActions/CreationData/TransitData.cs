using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record TransitData(
     Curve2D Centerline,
     double Width,
     TransitType TransitType) : ICreationData
{
    public Transit ToTransit()
    {
        return new Transit(Centerline, Width, TransitType);
    }
}
