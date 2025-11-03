using ArchSitePlan.Core.Models;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Pipeline.CreationData;
public record MixedUseBuildingData
    (List<FloorPlate> Floors,
    Mesh EnvelopeMesh);