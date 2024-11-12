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

        public static List<BoundedCurve> ToHypar(this ICurve curve)
        {
            List<BoundedCurve> hyparCrvs = new List<BoundedCurve>();
            var curves = new List<ICurve>();

            if (curve == null) return hyparCrvs;

            if (curve is PolyCurve) 
                curves.AddRange((curve as PolyCurve).Curves);
            else
                curves.Add(curve);


            foreach (var crv in curves)
            {
                Line line = crv as Line;
                if (line != null)
                    hyparCrvs.Add(ToHypar(line));

                Polyline pline = crv as Polyline;
                if (pline != null)
                    hyparCrvs.Add(ToHypar(pline));

                Arc arc = crv as Arc;
                if (arc != null)
                    hyparCrvs.Add(ToHypar(arc));
            }

            return hyparCrvs;
        }

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
