using AIQ.Urban.Core.Models;
using Rhino.Geometry;

namespace AIQ.Urban.Core.ModelActions.CreationData;

public record BuildingUpdateData(
    Guid OldEnvelopeId,
    Brep NewEnvelope,
    double TypicalFloorHeight,
    List<Use> OldPlateUses) : ICreationData;
