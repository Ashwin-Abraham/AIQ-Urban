using BH.Engine.Geometry;
using BH.oM.Geometry;
namespace ArchSitePlan.BHoM.Geometry.Extensions;

public static class ArcExtensions
{
    public static Point Mid(this Arc arc) => Query.PointAtParameter(arc, 0.5);
}