using ArchSitePlan.Core.Models;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models.CreationData;
public record MixedUseBuildingData
    (List<FloorPlate> Floors,
    Mesh EnvelopeMesh);