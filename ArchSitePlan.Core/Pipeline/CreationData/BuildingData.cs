using BH.oM.Geometry;

namespace ArchSitePlan.Core.Pipeline.CreationData;

public record BuildingData(
    Mesh? Envelope,
    List<FloorplateData> Floors,
    Guid EnvelopeId,
    Guid BaseFloorId,
    double FloorHt);
