using ArchSitePlan.Core.Extensions;
using ArchSitePlan.DebugModel;
using ArchSitePlan.DebugModel.BHoMExtensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Test
{
    public class ICurveTests
    {
        Polygon _rectangle = Create.Polygon(new()
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
                Create.Point(2,-1),
                Create.Point(2,11));

            var fragments = (_rectangle as ICurve).Split(new List<ICurve>() { splitter });
            
            DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(_rectangle));
            DEBUGMODEL.AddBoundedCurves(ToHyparInterop.ToHyparPolylines(fragments), DebugMaterials.Blue, 0.5);
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

            var fragments = (_rectangle).Split(splitter.ObjectToList<ICurve>());

            DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(_rectangle));
            DEBUGMODEL.AddBoundedCurves(ToHyparInterop.ToHyparPolylines(fragments), DebugMaterials.Blue, 0.5);
            DEBUGMODEL.OpenAsGlb();
        }
        [Fact]
        public void CanSplitCurve_SameCurveTwice()
        {
            var splitter = _rectangle;

            var fragments = (_rectangle).Split(splitter.ObjectToList<ICurve>());

            DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(_rectangle));
            DEBUGMODEL.AddBoundedCurves(ToHyparInterop.ToHyparPolylines(fragments), DebugMaterials.Blue, 0.5);
            DEBUGMODEL.OpenAsGlb();

            Assert.Equal(1, fragments.Count);
            Assert.Equal(Math.Abs(_rectangle.Area()),
                Math.Abs(fragments[0].Area()));
        }
        [Fact]
        public void CanSplitCurve_WithPolyline2()
        {
            var splitter = Create.Polyline(new Point[] {
                Create.Point(4, -1),
                Create.Point(1.00031, 5),
                Create.Point(10, 5),
            });

            var fragments = (_rectangle).Split(splitter.ObjectToList<ICurve>());

            DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(_rectangle));
            DEBUGMODEL.AddBoundedCurves(ToHyparInterop.ToHyparPolylines(fragments), DebugMaterials.Blue, 0.5);
            DEBUGMODEL.OpenAsGlb();

            Assert.Equal(2, fragments.Count);
        }

        [Fact]
        public void CanSplitCurve_SplitterOutside()
        {
            var splitter = Create.Line(
                Create.Point(12, -1),
                Create.Point(12, 11));

            var fragments = (_rectangle as ICurve)
                .Split(new List<ICurve>() { splitter });

            DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(_rectangle));
            DEBUGMODEL.AddBoundedCurves(ToHyparInterop.ToHyparPolylines(fragments), DebugMaterials.Blue, 0.5);
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
            DEBUGMODEL.AddBoundedCurves(ToHyparInterop.ToHyparPolylines(fragments), DebugMaterials.Blue, 0.5);
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
            DEBUGMODEL.AddBoundedCurves(ToHyparInterop.ToHyparPolylines(fragments), DebugMaterials.Blue, 0.5);
            DEBUGMODEL.OpenAsGlb();
        }

        [Fact]
        public void CanIOffset_ThickenedLine()
        {
            var line = Create.Line(
                Create.Point(2, 0),
                Create.Point(2, 10));

            var offset = line.Thicken(1, Enums.EndCapType.Butt, Vector.ZAxis);


            // Visualize
            DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(line));
            DEBUGMODEL.AddBoundedCurves(ToHyparInterop.ToHypar(offset), DebugMaterials.Red);
            DEBUGMODEL.OpenAsGlb();

            // Assert
            Assert.Equal(24, offset.ILength());
        }

        static Line _line1 = Create.Line(Point.Origin, Create.Point(0, 2, 0));
        static Line _line2 = Create.Line(Create.Point(2, 0, 0), Point.Origin);
        static Arc _arc = Create.ArcByCentre(Point.Origin, Create.Point(0, 2, 0), Create.Point(2, 0, 0));
        static List<ICurve> _pCrvList = new List<ICurve>() { _line2, _line1, _arc};

        static PolyCurve _arcPolyCurve = Create.PolyCurve(_pCrvList);

        [Fact]
        public void CanSplitPolycurve_WithSelf()
        {
            var curve = _arcPolyCurve;
            var splitter = _arcPolyCurve;

            var fragments = (curve as ICurve).Split(new List<ICurve>() { splitter });

#if DEBUG
            VisualizeSplitResults(curve, splitter, fragments);
#endif
        }

        [Fact]
        public void CanSplitPolycurve_WithLine()
        {
            var curve = _arcPolyCurve;
            var splitter = Create.Line(Point.Origin, Create.Point(5,5));

            var fragments = (curve as ICurve).Split(new List<ICurve>() { splitter });

#if DEBUG
            VisualizeSplitResults(curve, splitter, fragments);
#endif
            Assert.Equal(2, fragments.Count);
            Assert.Equal(curve.Area(), fragments.Sum(f => f.Area()), 1E-04);
        }

        [Fact]
        public void CanSplitPolycurve_WithLine2()
        {
            var curve = _arcPolyCurve;
            var splitter = Create.Line(Create.Point(1,-1), Create.Point(1, 5));

            var fragments = (curve as ICurve).Split(new List<ICurve>() { splitter });

#if DEBUG
            VisualizeSplitResults(curve, splitter, fragments);
#endif
            Assert.Equal(2, fragments.Count);
            Assert.Equal(curve.Area(), fragments.Sum(f => f.Area()), 1E-04);
        }

        [Fact]
        public void CanSplitPolycurve_WithLine3()
        {
            var curve = _arcPolyCurve;
            var splitter = Create.Line(Create.Point(1, -1), Create.Point(1, 5));

            var fragments = (curve as ICurve).Split(new List<ICurve>() { splitter });

#if DEBUG
            VisualizeSplitResults(curve, splitter, fragments);
#endif
            Assert.Equal(2, fragments.Count);
            Assert.Equal(curve.Area(), fragments.Sum(f => f.Area()), 1E-04);
        }

        private static void VisualizeSplitResults(ICurve curve, 
            ICurve splitter, 
            IEnumerable<ICurve> fragments)
        {
#if DEBUG
            DEBUGMODEL.AddBoundedCurves(ToHyparInterop.ToHypar(splitter), DebugMaterials.Red);
            DEBUGMODEL.AddBoundedCurves(ToHyparInterop.ToHypar(curve));

            var hyparCrvs = fragments.SelectMany(f => ToHyparInterop.ToHypar(f))
                .Where(c => c is not null)
                .ToList();
            DEBUGMODEL.AddBoundedCurves(hyparCrvs, DebugMaterials.Blue, 0.01);
            DEBUGMODEL.OpenAsGlb();
#endif
        }


    }
}