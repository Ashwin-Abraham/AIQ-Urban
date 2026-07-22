using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record ParcelBoundaryData(Curve2D Curve) : ICreationData
{
    public ParcelBoundary ToParcelBoundary()
    {
        return new ParcelBoundary(Curve);
    }
}
