using ArchSitePlan.DebugModel;
using ArchSitePlan.Test.BHoMExtensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using BoundedCurve = Elements.Geometry.BoundedCurve;
using Vector3 = Elements.Geometry.Vector3;

namespace ArchSitePlan.Test
{
    public class UnitTest1
    {
        [Fact]
        public void CanOffsetPolycurve()
        {
            Line t3 = Create.Line(Point.Origin, Create.Point(0,5));
            Line offsetLine = t3.Offset(1, Vector.XAxis);

            DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(t3));
            DEBUGMODEL.AddBoundedCurve(ToHyparInterop.ToHypar(offsetLine), DebugMaterials.Red);
            DEBUGMODEL.OpenAsGlb();

            Arc t2 = Create.RandomArc(Point.Origin);
            
            var join = Compute.IJoin(new List<ICurve>(){ t2, t3 }).First();
            double length1 = join.ILength();

            var projectedCrv = Modify.Project(join, Plane.XY);

            List<BoundedCurve> crvs2 = ToHyparInterop.ToHypar(projectedCrv);
            
            
            var offsetCrv = Modify.Offset(join, 0.5);
            List<BoundedCurve> crvs1 = ToHyparInterop.ToHypar(offsetCrv);

            DEBUGMODEL.AddBoundedCurves(crvs1, DebugMaterials.Red);
            DEBUGMODEL.OpenAsGlb();

        }

        [Fact]
        public void CanOpenDebugModel()
        {
            var line = new Elements.Geometry.Line(Vector3.Origin, new Vector3(25,0));

            DEBUGMODEL.AddBoundedCurve(line);
            DEBUGMODEL.OpenAsGlb();
        }
    }
}