using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.ModelActions.EditData;

public record BuildingMassData(
    BrepElement Envelope,
    double FloorHeight,
    Use Use) : IModelEditData;
