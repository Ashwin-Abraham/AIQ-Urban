using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record ZoneData(Polygon2D Boundary, string Name) : ICreationData
{
    public Zone ToZone()
    {
        return new Zone(Boundary, Name);
    }
}
