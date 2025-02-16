using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Elements;
using Elements.Geometry;
using Circle = Elements.Geometry.Circle;

namespace ArchSitePlan.Test;
internal static class TestVisualizers
{
    internal static List<Element> ToElements(this IEnumerable<ICurve> curves)
    {
        return curves
            .ToHypar()
            .Select(x => new ModelCurve(x))
            .Cast<Element>()
            .ToList();
    }

    internal static void Visualize(this IEnumerable<Element> elements)
    {
#if DEBUG
        DEBUGMODEL.Reset();
        DEBUGMODEL.AddElements(elements);
        DEBUGMODEL.OpenAsGlb();
#endif
    }

    internal static void VisualizeSplitResults(ICurve curve,
        ICurve splitter,
        IEnumerable<PlanarSurface> fragments)
    {
        var hyparCrvs = fragments
            .Select(srf => srf.ToHypar())
            .SelectMany(x => x.Perimeter.Concat(x.Voids.SelectMany(v =>v)))
            .ToList();

        Visualize(curve, splitter, hyparCrvs);
    }

    internal static void VisualizeSplitResults(ICurve curve,
        ICurve splitter,
        IEnumerable<ICurve> fragments,
        double pointRadius = 0.2)
    {
        var hyparCrvs = fragments.SelectMany(f => f.ToHypar())
            .Where(c => c is not null)
            .ToList();

        var centers = fragments
            .SelectMany(crv => new[] { crv.IStartPoint(), crv.IEndPoint() })
            .ToList();

        var circles = centers
            .Select(pt => Create.Circle(pt, pointRadius))
            .Select(crv => crv.ToHypar())
            .ToList();

        
        Visualize(curve, splitter, hyparCrvs, circles);
    }

    private static void Visualize(ICurve curve,
        ICurve splitter,
        IEnumerable<BoundedCurve> hyparCrvs,
        IEnumerable<Circle> circles = null)
    {
        circles ??= new List<Circle>();


#if DEBUG
        DEBUGMODEL.Reset();
        DEBUGMODEL.AddBoundedCurves(splitter.ToHypar(), DebugMaterials.Red);
        DEBUGMODEL.AddBoundedCurves(curve.ToHypar());
        DEBUGMODEL.AddBoundedCurves(hyparCrvs, DebugMaterials.Blue, 0.01);
        DEBUGMODEL.AddBoundedCurves(circles.Select(c => c.ToPolygonalCircle()), DebugMaterials.Blue, 0.01);
        DEBUGMODEL.OpenAsGlb();
#endif
    }
}
