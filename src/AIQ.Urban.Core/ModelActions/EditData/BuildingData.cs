using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.ModelActions.EditData;

public record BuildingFootprintData(
    Polygon2D Footprint,
    double TypicalFloorHeight,
    int FloorCount,
    Use Use) : IModelEditData;
