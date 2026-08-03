using AIQ.Urban.Core.Models;
using Rhino.Geometry;

namespace AIQ.Urban.Core.ModelActions.EditData;

public record BuildingUpdateData(
    Guid OldEnvelopeId,
    Brep NewEnvelope,
    double TypicalFloorHeight,
    List<Use> OldPlateUses) : IModelEditData;
