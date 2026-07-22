using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record BuildingFootprintData(
    Polycurve2D Footprint,
    double TypicalFloorHeight,
    int FloorCount,
    Use Use) : ICreationData;
