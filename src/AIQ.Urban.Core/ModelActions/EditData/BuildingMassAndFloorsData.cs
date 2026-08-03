using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;

namespace AIQ.Urban.Core.ModelActions.EditData;

public record BuildingMassAndFloorsData(
    BrepElement Envelope,
    IReadOnlyList<FloorPlate> AssociatedPlates) : IModelEditData;
