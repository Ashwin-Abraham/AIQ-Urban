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
            PolyCurve crv = Create.RandomPolyCurve();
            var newCrv = Modify.Offset(crv, 1);

            List<BoundedCurve> crvs1 = ToHyparInterop.ToHypar(crv);
            //List<BoundedCurve> crvs2 = ToHyparInterop.ToHypar(newCrv);

            DEBUGMODEL.AddBoundedCurves(crvs1);
            //DEBUGMODEL.AddBoundedCurves(crvs2);
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