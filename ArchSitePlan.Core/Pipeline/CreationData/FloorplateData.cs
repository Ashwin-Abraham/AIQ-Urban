using ArchSitePlan.Core.Models;

namespace ArchSitePlan.Core.Pipeline.CreationData;
public record FloorplateData(
    Guid Id,
    Landuse? Use);
