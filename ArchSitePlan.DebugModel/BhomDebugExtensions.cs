using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.oM.Geometry;
using Elements;

namespace ArchSitePlan.DebugModel;

public static class BhomDebugExtensions
{
    public static void Debug(this ICurve curve, Material? material = null, double zMovt = 0)
        => curve.ToHypar().Debug(material, zMovt);

    public static void Debug(this IEnumerable<ICurve> curves, Material? material = null, double zMovt = 0)
        => curves.ToHypar().Debug(material, zMovt);

    public static void Debug(this Point point, double radius = 0.15, Material? material = null, double zMovt = 0)
        => point.ToHypar().Debug(radius, material, zMovt);

    public static void Debug(this IEnumerable<Point> points, Material? material = null, double zMovt = 0)
        => points.Debug(material, zMovt);
}
