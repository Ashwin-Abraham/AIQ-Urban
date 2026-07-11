using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record BuildingMassData(
    BrepElement Envelope,
    double FloorHeight,
    Use Use) : ICreationData;
