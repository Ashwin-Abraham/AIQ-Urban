using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models.CreationData;

public record BuildingData(
    Mesh? Envelope,
    List<FloorplateData> Floors,
    Guid EnvelopeId,
    Guid BaseFloorId,
    double FloorHt);
