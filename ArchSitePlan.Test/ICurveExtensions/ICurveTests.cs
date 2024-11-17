using ArchSitePlan.Core.Extensions;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using static ArchSitePlan.Test.ICurveExtensions.CurvesTestData;

namespace ArchSitePlan.Test.ICurveExtensions
{
    public class ICurveTests
    {
        [Fact]
        public void CanIOffset_ThickenedLine()
        {
            var line = Create.Line(
                Create.Point(2, 0),
                Create.Point(2, 10));

            var offset = line.Thicken(1, Enums.EndCapType.Butt, Vector.ZAxis);


            // Visualize
            DEBUGMODEL.AddBoundedCurve(line.ToHypar());
            DEBUGMODEL.AddBoundedCurves(offset.ToHypar(), DebugMaterials.Red);
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

            DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(_rectangle));
            DEBUGMODEL.AddBoundedCurves(fragments.ToHyparPolylines(), DebugMaterials.Blue, 0.5);
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

            var fragments = _rectangle.Split(splitter.ObjectToList<ICurve>());

            DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(_rectangle));
            DEBUGMODEL.AddBoundedCurves(fragments.ToHyparPolylines(), DebugMaterials.Blue, 0.5);
            DEBUGMODEL.OpenAsGlb();
        }
        [Fact]
        public void CanSplitCurve_SameCurveTwice()
        {
            var curve = _rectangle;
            var splitter = _rectangle;

            var fragments = curve.Split(splitter.ObjectToList<ICurve>());
            
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

            var fragments = curve.Split(splitter.ObjectToList<ICurve>());

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

            var fragments = curve.Split(splitter.ObjectToList<ICurve>());

            TestVisualizers.VisualizeSplitResults(_rectangle, splitter, fragments);

            Assert.Equal(4, fragments.Count);
            Assert.Equal(curve.ILength(), fragments.Sum(f => f.ILength()), 1E-03);
        }

        // Future additional tests as needed
        //public static IEnumerable<object[]> RectangleData =>
        //new List<object[]>
        //{
        //    new object[] { _rectangle, 4 } 
        //};

        //[Theory]
        //[MemberData(nameof(RectangleData))] 
        //public void CanSplitCurve_Regressions(ICurve curve, int expectedCount)
        //{
        //    
        //}

        [Fact]
        public void CanSplitCurve_SplitterOutside()
        {
            var splitter = Create.Line(
                Create.Point(12, -1),
                Create.Point(12, 11));

            var fragments = (_rectangle as ICurve)
                .Split(new List<ICurve>() { splitter });

            DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(_rectangle));
            DEBUGMODEL.AddBoundedCurves(fragments.ToHyparPolylines(), DebugMaterials.Blue, 0.5);
            DEBUGMODEL.OpenAsGlb();
        }

        [Fact]
        public void CanSplitCurve_SplitterOnEdge()
        {
            var splitter = Create.Line(
                Create.Point(5, -1),
                Create.Point(5, 11));

            var fragments = (_rectangle as ICurve).Split(new List<ICurve>() { splitter });

            DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(_rectangle));
            DEBUGMODEL.AddBoundedCurves(fragments.ToHyparPolylines(), DebugMaterials.Blue, 0.5);
            DEBUGMODEL.OpenAsGlb();
        }

        [Fact]
        public void CanSplitCurve_SplitterVertsOnEdge()
        {
            var splitter = Create.Line(
                Create.Point(2, 0),
                Create.Point(2, 10));

            var fragments = (_rectangle as ICurve).Split(new List<ICurve>() { splitter });

            DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(_rectangle));
            DEBUGMODEL.AddBoundedCurves(fragments.ToHyparPolylines(), DebugMaterials.Blue, 0.5);
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