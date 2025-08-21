using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.oM.Geometry;
using Elements;

namespace ArchSitePlan.DebugModel;

public static class BhomDebugExtensions
{
    public static List<Element> Debug(this ICurve curve, Material? material = null, double zMovt = 0)
        => curve.ToHypar().Debug(material, zMovt);

    public static List<Element> Debug(this IEnumerable<ICurve> curves, Material? material = null, double zMovt = 0)
        => curves.ToHypar().Debug(material, zMovt);

    public static Element Debug(this Point point, double radius = 0.15, Material? material = null, double zMovt = 0)
        => point.ToHypar().Debug(radius, material, zMovt);

    public static List<Element> Debug(this IEnumerable<Point> points, double radius = 0.15, Material? material = null, double zMovt = 0)
        => points.Select(x => x.Debug(radius, material, zMovt)).ToList();
}
