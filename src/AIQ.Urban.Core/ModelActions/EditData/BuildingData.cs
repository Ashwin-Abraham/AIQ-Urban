using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.ModelActions.EditData;

public record BuildingFootprintData : IModelEditData
{
    public Polygon2D Footprint { get; }
    public double TypicalFloorHeight { get; }
    public int FloorCount { get; }
    public Use Use { get; }

    public BuildingFootprintData(
        Polygon2D footprint,
        double typicalFloorHeight,
        int floorCount,
        Use use)
    {
        ArgumentNullException.ThrowIfNull(footprint);
        if (footprint.Geometry.IsSelfIntersecting())
            throw new ArgumentException("Building footprint must not self-intersect.", nameof(footprint));

        Footprint = footprint;
        TypicalFloorHeight = typicalFloorHeight;
        FloorCount = floorCount;
        Use = use;
    }
}
