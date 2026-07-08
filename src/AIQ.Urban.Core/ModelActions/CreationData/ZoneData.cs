using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record ZoneData(Polygon2D Boundary, string Name) : ICreationData
{
    public Zone ToZone()
    {
        return new Zone(Boundary, Name);
    }
}
