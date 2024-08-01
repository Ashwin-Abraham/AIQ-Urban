using Elements.Geometry;
using System.Collections.Generic;
using System.Linq;
using Arc = Elements.Geometry.Arc;

namespace AA.RhinoHyparInterop
{
    public static class RhinoToHypar
    {
        public static Arc ToHyparArc(this Rhino.Geometry.Curve curve)
        {
            if (!curve.TryGetArc(out Rhino.Geometry.Arc arc))
                return null;

            Arc hyparArc = new Arc(center: arc.Center.ToHypar(),
                radius: arc.Radius,
                startAngle: arc.StartAngle,
                endAngle: arc.EndAngle);

            return hyparArc;
        }

        public static Line ToHyparLine(this Rhino.Geometry.Curve curve)
        {
            return new Elements.Geometry.Line(curve.PointAt(0).ToHypar(),
                                                curve.PointAt(1).ToHypar());
        }

        public static Vector3 ToHypar(
            this Rhino.Geometry.Point3d v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static List<BoundedCurve> ToHyparArcsAndLines(this Rhino.Geometry.Curve curve)
        {
            List<BoundedCurve> hyparCrvs = new List<BoundedCurve>();
            var segments = curve.ToArcsAndLines(0.001, 0.001, 0.001, 100000000)
                .Explode()
                .ToList();

            foreach(var crv in segments)
            {
                if(crv.IsArc())
                {
                    var arc = crv.ToHyparArc();
                    if(arc != null)
                        hyparCrvs.Add(arc);
                }

                else if(crv.IsLinear())
                {
                    var line = crv.ToHyparLine();
                    if(line != null)
                        hyparCrvs.Add(line);
                }
            }
                
            return hyparCrvs;
        }

        public static IndexedPolycurve ToHyparPolycurve(this Rhino.Geometry.Curve curve)
        {
            List<BoundedCurve> hyparCrvs = curve.ToHyparArcsAndLines();

            return new IndexedPolycurve(hyparCrvs);
        }
    }
}