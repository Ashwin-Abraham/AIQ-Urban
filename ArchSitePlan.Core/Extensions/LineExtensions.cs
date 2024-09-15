using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Core.Extensions
{
    public static class LineExtensions
    {
        /// <summary>
        /// Works only in XY plane
        /// Generates an arc between Line a End and Line b start
        /// </summary>
        public static PolyCurve ArcBetweenLinesThatMeet(
            this Line a,
            Line b,
            double radius,
            double tolerance = 1E-06)
        {
            double angleBetweenLines = a.SignedAngle(b, Vector.ZAxis);
            double trim = Math.Abs(Math.Tan(angleBetweenLines / 2) * radius);
            double distanceOfArcCenterFromIntersect = radius / (Math.Cos(angleBetweenLines / 2));

            if (a.Length() < trim
                || b.Length() < trim
                || a.End.Distance(b.Start) > tolerance)
                return Create.PolyCurve(new List<Line>() { a, b });

            var bisectorDirection = (a.Direction().Reverse() + b.Direction()) / 2;
            var arcCenter = a.End + bisectorDirection * distanceOfArcCenterFromIntersect;
            var arcMid = arcCenter + bisectorDirection.Reverse() * radius;

            Line shortA = a.Extend(0, -trim);
            Line shortB = b.Extend(-trim, 0);
            Arc arc = Create.Arc(shortA.End, arcMid, shortB.Start);

            PolyCurve polyCurve = new PolyCurve();
            polyCurve.Curves.Add(shortA);
            polyCurve.Curves.Add(arc);
            polyCurve.Curves.Add(shortB);
            return polyCurve;
        }
    }
}
