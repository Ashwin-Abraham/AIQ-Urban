using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions.EditData;

public record ZoneData(Polygon2D Boundary, string Name) : IModelEditData
{
    public Zone ToZone()
    {
        return new Zone(Boundary, Name);
    }
}
