using BH.Engine.Geometry;
using BH.oM.Geometry;
using BoundedCurve = Elements.Geometry.BoundedCurve;

namespace ArchSitePlan.DebugModel.BHoMExtensions
{
    public static class ToHyparInterop
    {
        public static Elements.Geometry.Polyline ToHyparPolyline(this ICurve curve)
        {
            var verts = curve.IControlPoints()
                .Select(pt => pt.ToHypar())
                .ToList();

            return new Elements.Geometry.Polyline(verts);
               
        }

        public static IEnumerable<Elements.Geometry.Polyline> ToHyparPolylines(
            this IEnumerable<ICurve> curves)
        {
            return curves.Select(crv => crv.ToHyparPolyline());
        }
        public static (List<BoundedCurve> Perimeter, List<List<BoundedCurve>> Voids) ToHypar(
            this PlanarSurface surface)
        {
            var perimeter = surface.ExternalBoundary.ToHypar();
            var voids = surface.InternalBoundaries.Select(v => v.ToHypar()).ToList();

            return (perimeter, voids);
        }

        public static List<BoundedCurve> ToHypar(this IEnumerable<ICurve> curves)
            => curves.SelectMany(x => x.ToHypar()).ToList();

        public static List<BoundedCurve> ToHypar(this ICurve crv)
        {
            List<BoundedCurve> hyparCrvs = new List<BoundedCurve>();

            if (crv is Line l)
            {
                var line = ToHypar(l);
                if(line is not null) hyparCrvs.Add(line);
            }

            else if (crv is Polyline pline)
                hyparCrvs.Add(ToHypar(pline));

            else if (crv is Arc arc)
                hyparCrvs.Add(ToHypar(arc));

            else if (crv is Circle circle)
                hyparCrvs.Add(ToHypar(circle));

            else if (crv is Polygon polygon)
                hyparCrvs.Add(ToHypar(polygon));

            else if (crv is Polyline polyline)
                hyparCrvs.Add(ToHypar(polyline));

            else if (crv is PolyCurve polyCurve)
                hyparCrvs.AddRange(ToHypar(polyCurve));

            else if (crv is BoundaryCurve boundary)
                hyparCrvs.AddRange(ToHypar(boundary));

            return hyparCrvs;
        }

        public static List<BoundedCurve> ToHypar(this BoundaryCurve boundaryCurve)
            => boundaryCurve.Curves.SelectMany(crv => crv.ToHypar()).ToList();

        public static List<BoundedCurve> ToHypar(this PolyCurve polycurve)
            => polycurve.Curves.SelectMany(crv => crv.ToHypar()).ToList();

        public static Elements.Geometry.Circle ToHypar(this Circle circle)
            =>new Elements.Geometry.Circle(circle.Centre.ToHypar(), circle.Radius);

        public static Elements.Geometry.Arc ToHypar(this Arc arc)
        {
            var arcTransform = new Elements.Geometry.Transform(
                origin: arc.CoordinateSystem.Origin.ToHypar(),
                xAxis: arc.CoordinateSystem.X.ToHypar(),
                yAxis: arc.CoordinateSystem.Y.ToHypar(),
                zAxis: arc.CoordinateSystem.Z.ToHypar());

            var hyparArc = new Elements.Geometry.Arc(
                arcTransform,
                arc.Radius,
                arc.StartAngle,
                arc.EndAngle);

            var test1 = arc.PointAtParameter(0.5);
            var test2 = hyparArc.PointAtNormalized(0.5);
            return hyparArc;
        }

        public static Elements.Geometry.Vector3 ToHypar(this Vector v)
        {
            return new Elements.Geometry.Vector3(v.X, v.Y, v.Z);
        }
        public static Elements.Geometry.Polyline ToHypar(this Polyline pline)
        {
            var points = pline.ControlPoints.Select(pt => ToHypar(pt)).ToList();

            return new Elements.Geometry.Polyline(points);
        }

        public static Elements.Geometry.Line? ToHypar(this Line line)
        {
            if(line.Start.Distance(line.End)< Elements.Geometry.Vector3.EPSILON) return null;

            return new Elements.Geometry.Line(ToHypar(line.Start), ToHypar(line.End));
        }

        public static Elements.Geometry.Vector3 ToHypar(this Point point)
        {
            return new Elements.Geometry.Vector3(point.X, point.Y, point.Z);
        }
    }
}
