using AQI.Urban.Core.Tests.TestUtils;
using ArchSitePlan.Core.Factories.Models;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Elements;
using Elements.Geometry;
using Circle = Elements.Geometry.Circle;

namespace AQI.Urban.Core.Tests.TestUtils;
public static class TestVisualizers
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
        elements.Debug();
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

    //public static void Visualize(this IEnumerable<ICurve> curves)
    //{
    //    curves.Debug();
    //    DEBUGMODEL.OpenAsGlb();
    //}

    public static void Debug(this PlanarSurface srf, Material? material = null)
    {
#if DEBUG
        srf.ToHyparMass(material: material).Debug();
#endif

    }
    public static void Debug(this IEnumerable<ICurve> curves)
        => curves.ToList().ForEach(x => x.Debug());
    
//    public static void Debug(this ICurve curve)
//    {
//#if DEBUG
//        DEBUGMODEL.Debug(curve.ToHypar());
//#endif
//    }
    private static void Visualize(ICurve curve,
        ICurve splitter,
        IEnumerable<BoundedCurve> hyparCrvs,
        IEnumerable<Circle> circles = null)
    {
        circles ??= new List<Circle>();


#if DEBUG
        DEBUGMODEL.Reset();
        splitter.ToHypar().Debug(DebugMaterials.Red);
        curve.ToHypar().Debug();
        hyparCrvs.Debug(DebugMaterials.Blue, 0.01);
        circles.Select(c => c.ToPolygonalCircle()).Debug(DebugMaterials.Blue, 0.01);
        DEBUGMODEL.OpenAsGlb();
#endif
    }

    public static void VisualizeBuildingLayout(BuildingLayout layout)
    {
        DEBUGMODEL.Reset();
        DebugBuildingLayout(layout);
        DEBUGMODEL.OpenAsGlb();
    }

    public static void DebugBuildingLayout(BuildingLayout layout)
    {
        var floors = layout.Buildings
            .SelectMany(x => x.FloorPlates)
            .SelectMany(x => x.Profile.InternalBoundaries
                .Append(x.Profile.ExternalBoundary));

        floors.ToHypar().Debug();
        layout.SiteBoundary.ToHypar().Debug();
        layout.SetBackBoundary.ToHypar().Debug();
        layout.Buildings.Select(x => x.Envelope.ToHypar()).Debug();
    }
}
