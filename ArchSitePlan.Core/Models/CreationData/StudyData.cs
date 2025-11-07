using BH.oM.Geometry;

namespace ArchSitePlan.Core.Models.CreationData
{
    public record StudyData(
            ICurve Boundary,
            Guid Id,
            string Name);
}
