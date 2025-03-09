using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using ArchSitePlan.Test;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Test.Geometry.Extensions.CurvesTestData;

namespace ArchSitePlan.Test.Geometry.Extensions
{
    public class ICurveExtensionsTests
    {
        [Fact]
        public void CanThicken_Polygon()
        {
            var polygon = Create.Polygon(new() {
                Create.Point(2, 0),
                Create.Point(2, 10),
                Create.Point(10,10),
                Create.Point(10,0)});

            var offset = polygon.Thicken(1, EndCapType.Butt, Vector.ZAxis);


            // Visualize
            polygon.ToHypar().Debug();
            offset.ToHypar().Debug(DebugMaterials.Red);
            DEBUGMODEL.OpenAsGlb();

            // Assert
            Assert.Equal(72, offset.ILength());
            Assert.Equal(6, offset.IControlPoints().Count);
        }

        [Fact]
        public void CanThicken_CollapseToPointPolygon()
        {
            var polygon = Create.Polygon(new() {
                Create.Point(0, 0),
                Create.Point(2, 0),
                Create.Point(2,2),
                Create.Point(0,2)});

            var offset = polygon.Thicken(1, EndCapType.Butt, Vector.ZAxis);


            // Visualize
            polygon.ToHypar().Debug();
            offset.ToHypar().Debug(DebugMaterials.Red);
            DEBUGMODEL.OpenAsGlb();

            // Assert
            Assert.Equal(16, offset.ILength());
            Assert.Equal(5, offset.IControlPoints().Count);
            Assert.True(offset is Polygon);
        }

        [Fact]
        public void CanThicken_PolyCurve()
        {
            var polyline = Create.Polyline(new List<Point>() {
                Create.Point(0, 0),
                Create.Point(2, 0),
                Create.Point(2,2)});

            var arc = Create.Arc(
                Create.Point(2, 2),
                Create.Point(1, 3),
                Create.Point(0, 2));

            var line = Create.Line(
                Create.Point(0, 2),
                Create.Point(0, 0));

            var pCurve = Create.PolyCurve([polyline, arc, line]);

            var offset = pCurve.Thicken(1, EndCapType.Butt, Vector.ZAxis);


            // Visualize
            pCurve.ToHypar().Debug();
            offset.ToHypar().Debug(DebugMaterials.Red);
            DEBUGMODEL.OpenAsGlb();

            // Assert
            Assert.Equal(16.2831, offset.ILength(), 1e-03);
            Assert.Equal(8, offset.IControlPoints().Count);
            Assert.True(offset is PolyCurve);
        }

        [Fact]
        public void CanIOffset_ThickenedLine()
        {
            var line = Create.Line(
                Create.Point(2, 0),
                Create.Point(2, 10));

            var offset = line.Thicken(1, EndCapType.Butt, Vector.ZAxis);


            // Visualize
            line.ToHypar().Debug();
            offset.ToHypar().Debug(DebugMaterials.Red);
            DEBUGMODEL.OpenAsGlb();

            // Assert
            Assert.Equal(24, offset.ILength());
        }

        static Polygon _rectangle = Create.Polygon(new()
            {
                Point.Origin,
                Create.Point(5,0),
                Create.Point(5,10),
                Create.Point(0,10)
            });

        [Fact]
        public void CanSplitCurve_Typical()
        {
            var splitter = Create.Line(
                Create.Point(2, -1),
                Create.Point(2, 11));

            var fragments = (_rectangle as ICurve).Split(new List<ICurve>() { splitter });

            ((Elements.Geometry.BoundedCurve)ToHyparInterop.ToHypar(_rectangle)).Debug();
            fragments.ToHyparPolylines().Debug(DebugMaterials.Blue, 0.5);
            DEBUGMODEL.OpenAsGlb();
        }

        [Fact]
        public void CanSplitCurve_WithPolyline1()
        {
            var splitter = Create.Polyline(new Point[] {
                Create.Point(4, -1),
                Create.Point(1, 5),
                Create.Point(4, 11),
            });

            var fragments = _rectangle.Split([splitter]);

            ((Elements.Geometry.BoundedCurve)ToHyparInterop.ToHypar(_rectangle)).Debug();
            fragments.ToHyparPolylines().Debug(DebugMaterials.Blue, 0.5);
            DEBUGMODEL.OpenAsGlb();
        }
        [Fact]
        public void CanSplitCurve_SameCurveTwice()
        {
            var curve = _rectangle;
            var splitter = _rectangle;

            var fragments = curve.Split([splitter]);
            
            TestVisualizers.VisualizeSplitResults(curve, splitter, fragments);
            Assert.Equal(1, fragments.Count);
            Assert.Equal(curve.ILength(), fragments.Sum(f => f.ILength()), 1E-03);
        }

        [Fact]
        public void CanSplitCurve_WithJaggedPolyline()
        {
            var curve = _rectangle;
            var splitter = Create.Polyline(new Point[] {
                Create.Point(4, -1),
                Create.Point(1.00031, 5),
                Create.Point(10, 5),
            });

            var fragments = curve.Split([splitter]);

            TestVisualizers.VisualizeSplitResults(_rectangle, splitter, fragments);
            
            Assert.Equal(2, fragments.Count);
            Assert.Equal(curve.ILength(), fragments.Sum(f => f.ILength()), 1E-03);
        }

        [Fact]
        public void CanSplitCurve_Into4Parts()
        {
            var curve = _rectangle;
            var splitter = Create.Polyline(new Point[] {
                Create.Point(4, -1),
                Create.Point(-2, 5),
                Create.Point(10, 5),
            });

            var fragments = curve.Split([splitter]);

            TestVisualizers.VisualizeSplitResults(_rectangle, splitter, fragments);

            Assert.Equal(4, fragments.Count);
            Assert.Equal(curve.ILength(), fragments.Sum(f => f.ILength()), 1E-03);
        }

        [Fact]
        public void CanSplitCurve_SplitterOutside()
        {
            var splitter = Create.Line(
                Create.Point(12, -1),
                Create.Point(12, 11));

            var fragments = (_rectangle as ICurve)
                .Split(new List<ICurve>() { splitter });

            ((Elements.Geometry.BoundedCurve)ToHyparInterop.ToHypar(_rectangle)).Debug();
            fragments.ToHyparPolylines().Debug(DebugMaterials.Blue, 0.5);
            DEBUGMODEL.OpenAsGlb();
        }

        [Fact]
        public void CanSplitCurve_SplitterOnEdge()
        {
            var splitter = Create.Line(
                Create.Point(5, -1),
                Create.Point(5, 11));

            var fragments = (_rectangle as ICurve).Split(new List<ICurve>() { splitter });

            ((Elements.Geometry.BoundedCurve)ToHyparInterop.ToHypar(_rectangle)).Debug();
            fragments.ToHyparPolylines().Debug(DebugMaterials.Blue, 0.5);
            DEBUGMODEL.OpenAsGlb();
        }

        [Fact]
        public void CanSplitCurve_SplitterVertsOnEdge()
        {
            var splitter = Create.Line(
                Create.Point(2, 0),
                Create.Point(2, 10));

            var fragments = (_rectangle as ICurve).Split(new List<ICurve>() { splitter });

            ((Elements.Geometry.BoundedCurve)ToHyparInterop.ToHypar(_rectangle)).Debug();
            fragments.ToHyparPolylines().Debug(DebugMaterials.Blue, 0.5);
            DEBUGMODEL.OpenAsGlb();
        }


        [Fact]
        public void CanSplitPolycurve_WithSelf()
        {
            var curve = ArcPolyCurve;
            var splitter = ArcPolyCurve;

            var fragments = (curve as ICurve).Split(new List<ICurve>() { splitter });

#if DEBUG
            TestVisualizers.VisualizeSplitResults(curve, splitter, fragments);
#endif
            Assert.Equal(1, fragments.Count);
            Assert.Equal(curve.ILength(), fragments.Sum(f => f.ILength()), 1E-03);
        }

        [Fact]
        public void CanSplitPolycurve_WithLine()
        {
            var curve = ArcPolyCurve;
            var splitter = Create.Line(Point.Origin, Create.Point(5, 5));

            var fragments = (curve as ICurve).Split(new List<ICurve>() { splitter });

#if DEBUG
            TestVisualizers.VisualizeSplitResults(curve, splitter, fragments);
#endif
            Assert.Equal(2, fragments.Count);
            Assert.Equal(curve.ILength(), fragments.Sum(f => f.ILength()), 1E-03);
        }

        [Fact]
        public void CanSplitPolycurve_WithLine2()
        {
            var curve = ArcPolyCurve;
            var splitter = Create.Line(Create.Point(1, -1), Create.Point(1, 5));

            var fragments = (curve as ICurve).Split(new List<ICurve>() { splitter });

#if DEBUG
            TestVisualizers.VisualizeSplitResults(curve, splitter, fragments);
#endif
            Assert.Equal(2, fragments.Count);
            Assert.Equal(curve.Area(), fragments.Sum(f => f.IArea()), 1E-04);
        }

        [Fact]
        public void CanSplitPolycurve_WithLine3()
        {
            var curve = ArcPolyCurve;
            var splitter = Create.Line(Create.Point(1, -1), Create.Point(1, 5));

            var fragments = (curve as ICurve).Split(new List<ICurve>() { splitter });

#if DEBUG
            TestVisualizers.VisualizeSplitResults(curve, splitter, fragments);
#endif
            Assert.Equal(2, fragments.Count);
            Assert.Equal(curve.Area(), fragments.Sum(f => f.IArea()), 1E-04);
        }

        


    }
}