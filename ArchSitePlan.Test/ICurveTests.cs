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

            var fragments = (_rectangle as ICurve).Split(new() { splitter });
            
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

            var fragments = (_rectangle as ICurve).Split(new() { splitter });

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

            var fragments = (_rectangle as ICurve).Split(new() { splitter });

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
    }
}