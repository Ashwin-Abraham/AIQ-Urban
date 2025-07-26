using ArchSitePlan.RhinoBhomInterop;
using NUnit.Framework;
using Rhino.Geometry;

namespace AIQ.RhinoTests.ArchSitePlam.RhinoBhomInteropTests.ToBhomTests
{
    public class CircleTests
    {
        [Test]
        public void RhinoCircleToBhom()
        {
            var circle = new Circle(new Point3d(0, 0, 0), 1).ToNurbsCurve();
            var bhom = circle.ToBHoM2DegreeCrv();

            Assert.That(bhom is BH.oM.Geometry.Circle);
        }
    }
}
