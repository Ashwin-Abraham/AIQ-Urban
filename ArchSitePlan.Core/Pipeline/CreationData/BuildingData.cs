using BH.oM.Geometry;

namespace ArchSitePlan.Core.Pipeline.CreationData
{
    public record BuildingData(
        Mesh? Envelope,
        List<PlanarSurface> Floors,
        Guid EnvelopeId,
        Guid FloorId,
        double FloorHt);
}
