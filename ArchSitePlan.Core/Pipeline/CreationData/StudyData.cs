using BH.oM.Geometry;

namespace ArchSitePlan.Core.Pipeline.CreationData
{
    public record StudyData(
            ICurve Boundary,
            Guid Id,
            string Name);
}
