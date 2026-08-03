using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions.EditData;

public record ParcelBoundaryData(Curve2D Curve) : IModelEditData
{
    public ParcelBoundary ToParcelBoundary()
    {
        return new ParcelBoundary(Curve);
    }
}
