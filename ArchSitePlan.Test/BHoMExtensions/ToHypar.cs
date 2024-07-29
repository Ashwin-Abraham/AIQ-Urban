using BH.oM.Geometry;
using BoundedCurve = Elements.Geometry.BoundedCurve;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Test.BHoMExtensions
{
    public static class ToHyparInterop
    {
        public static List<BoundedCurve> ToHypar(this PolyCurve polyCurve)
        {
            var curves = polyCurve.Curves;
            List<BoundedCurve> hyparCrvs = new List<BoundedCurve>();

            foreach (var crv in curves)
            {
                Line line = crv as Line;
                if (line != null)
                    hyparCrvs.Add(ToHypar(line));

                Polyline pline = crv as Polyline;
                if (pline != null)
                    hyparCrvs.Add(ToHypar(pline));

                //Arc arc = crv as Arc;
                //if (arc != null)
                //    hyparCrvs.Add(ToHyparInterop.ToHypar(arc));
            }

            return hyparCrvs;
        }
        public static Elements.Geometry.Polyline ToHypar(this Polyline pline)
        {
            var points = pline.ControlPoints.Select(pt => ToHypar(pt)).ToList();

            return new Elements.Geometry.Polyline(points);
        }

        public static Elements.Geometry.Line ToHypar(Line line)
        {
            return new Elements.Geometry.Line(ToHypar(line.Start), ToHypar(line.End));
        }

        public static Elements.Geometry.Vector3 ToHypar(Point point)
        {
            return new Elements.Geometry.Vector3(point.X, point.Y, point.Z);
        }
    }
}
