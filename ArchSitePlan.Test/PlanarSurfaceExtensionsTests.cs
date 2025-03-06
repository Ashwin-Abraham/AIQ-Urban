using ArchSitePlan.BHoM.Geometry.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Test.ICurveExtensions.CurvesTestData;

namespace ArchSitePlan.Test;
public class PlanarSurfaceExtensionsTests
{
    [Fact]
    public void CanSplitSurface_WithLine()
    {
        var curve = ArcPolyCurve;
        var planarSurface = Create.PlanarSurface(curve);
        var splitter = Create.Line(Create.Point(1, -1), Create.Point(1, 5));

        List<PlanarSurface> fragments = planarSurface.Split([splitter]);

        //var fragments = (curve as ICurve).Split(new List<ICurve>() { splitter });

#if DEBUG
        TestVisualizers.VisualizeSplitResults(curve, splitter, fragments);
#endif
        Assert.Equal(2, fragments.Count);
        Assert.Equal(curve.Area(), fragments.Sum(f => f.Area()), 1E-04);
    }

    [Fact]
    public void CanSplitSurface_WithPolygon_TerminalsOnEdge()
    {
        var curve = Rectangle1;
        var planarSurface = Create.PlanarSurface(curve);
        var splitter = Rectangle3;

        List<PlanarSurface> fragments = planarSurface.Split([splitter]);

#if DEBUG
        TestVisualizers.VisualizeSplitResults(curve, splitter, fragments);
#endif
        Assert.Equal(3, fragments.Count);
        Assert.Equal(curve.Area(), fragments.Sum(f => f.Area()), 1E-04);
    }

    [Fact]
    public void CanSplitSurface_WithPolygon_EdgeOnSrf()
    {
        var curve = Rectangle1;
        var planarSurface = Create.PlanarSurface(curve);
        var splitter = Rectangle4;

        List<PlanarSurface> fragments = planarSurface.Split([splitter]);

#if DEBUG
        TestVisualizers.VisualizeSplitResults(curve, splitter, fragments);
#endif
        Assert.Equal(1, fragments.Count);
        Assert.Equal(curve.Area(), fragments.Sum(f => f.Area()), 1E-04);
    }

    [Fact]
    public void CanSplitSurface_WithLine_NotASplit()
    {
        var curve = ArcPolyCurve;
        var planarSurface = Create.PlanarSurface(curve);
        var splitter = Create.Line(Create.Point(1, 1), Create.Point(1, 5));

        List<PlanarSurface> fragments = planarSurface.Split([splitter]);

#if DEBUG
        TestVisualizers.VisualizeSplitResults(curve, splitter, fragments);
#endif
        Assert.Single(fragments);
        Assert.Equal(7, fragments[0].ExternalBoundary.IControlPoints().Count());
        Assert.Equal(curve.Area(), fragments.Sum(f => f.Area()), 1E-04);
    }

    [Fact]
    public void CanSplitSurface_WithPolygon()
    {
        var curve = ArcPolyCurve;
        var planarSurface = Create.PlanarSurface(curve);
        var splitter = DiffPolygon;

        List<PlanarSurface> fragments = planarSurface.Split([splitter]);
#if DEBUG
        TestVisualizers.VisualizeSplitResults(curve, splitter, fragments);
#endif
        Assert.Equal(2, fragments.Count);
        Assert.Equal(curve.Area(), fragments.Sum(f => f.Area()), 1E-04);
    }

    [Fact]
    public void CanSplitSurface_WithComplexPolyline()
    {
        var curve = ArcPolyCurve;
        var planarSurface = Create.PlanarSurface(curve);
        var splitter = DiffPolyline;

        List<PlanarSurface> fragments = planarSurface.Split([splitter]);
#if DEBUG
        TestVisualizers.VisualizeSplitResults(curve, splitter, fragments);
#endif
        Assert.Equal(2, fragments.Count);
        Assert.Equal(curve.Area(), fragments.Sum(f => f.Area()), 1E-04);
    }

    [Fact]
    public void CanSplitSurface_WithComplexPolyline2()
    {
        var curve = ArcPolyCurve;
        var planarSurface = Create.PlanarSurface(curve);
        var splitter = DiffPolyline2;

        List<PlanarSurface> fragments = planarSurface.Split([splitter]);
#if DEBUG
        TestVisualizers.VisualizeSplitResults(curve, splitter, fragments);
#endif
        Assert.Equal(3, fragments.Count);
        Assert.Equal(curve.Area(), fragments.Sum(f => f.Area()), 1E-04);
    }

    [Fact]
    public void CanSplitHoledSurface_WithLine()
    {
        var curve = ArcPolyCurve;
        var hole = Hole as ICurve;
        var planarSurface = Create.PlanarSurface(curve, new() { hole });
        var splitter = Create.Line(Create.Point(1, -1), Create.Point(1, 5));

        List<PlanarSurface> fragments = planarSurface.Split([splitter]);

#if DEBUG
        TestVisualizers.VisualizeSplitResults(curve, splitter, fragments);
#endif
        Assert.Equal(2, fragments.Count);
        Assert.Equal(planarSurface.Area(), fragments.Sum(f => f.Area()), 1E-04);
    }

    [Fact]
    public void CanSplitSurface_WithPolygon_IsolatedFragments()
    {
        var curve = Rectangle1;
        var planarSurface = Create.PlanarSurface(curve);
        var splitter = Rectangle2;

        List<PlanarSurface> fragments = planarSurface.Split([splitter]);
#if DEBUG
        TestVisualizers.VisualizeSplitResults(curve, splitter, fragments);
#endif
        Assert.Equal(3, fragments.Count);
        Assert.Equal(curve.Area(), fragments.Sum(f => f.Area()), 1E-04);
    }
}
