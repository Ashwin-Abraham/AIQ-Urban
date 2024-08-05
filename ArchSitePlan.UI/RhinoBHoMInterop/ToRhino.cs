using ArchSitePlan.DebugModel;
using BH.Engine.Geometry;
using BH.Engine.Spatial;
using BH.oM.Geometry;

namespace ArchSitePlan.UI.RhinoBHoMInterop
{
    public static class ToRhinoInterop
    {
        public static Rhino.Geometry.Brep ToRhino(this PlanarSurface bhomSurface)
        {
            var rhinoPoints = bhomSurface.ExternalBoundary.IControlPoints().ToRhino();

            var brep = Rhino.Geometry.Brep.CreatePlanarBreps(bhomSurface.ExternalBoundary.ToRhino()).First();

            return brep;
        }

        public static Rhino.Geometry.Point3d ToRhino(this Point point)
        {
            return new Rhino.Geometry.Point3d(point.X, point.Y, point.Z);
        }

        public static IEnumerable<Rhino.Geometry.Point3d> ToRhino(this IEnumerable<Point> points)
        {
            return points.Select(pt => pt.ToRhino());
        }

        public static Rhino.Geometry.Curve ToRhino(this ICurve curve)
        {
            var rhinoPts = curve.IControlPoints().ToRhino();
            var rhinoCrv = Rhino.Geometry.Curve.CreateControlPointCurve(rhinoPts, 1);

            return rhinoCrv;
        }
    }
}
