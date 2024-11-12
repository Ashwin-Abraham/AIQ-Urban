using BH.Engine.Geometry;
using BH.oM.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Extensions
{
    public static class PolyCurveExtensions
    {
        public static bool ValidateContinuity(this PolyCurve curve)
        {
            var segments = curve.Curves;

            //if (segments.Exists(s => s is PolyCurve))
            //    return false;

            var toTest = segments.Where(s => s is not Polygon).ToList();

            for (int i = 0; i < toTest.Count - 1; i++)
            {
                var currentCrvEnd = toTest[i].IEndPoint();
                var nextCrvStart = toTest[i + 1].IStartPoint();

                if (!currentCrvEnd.IsEqual(nextCrvStart))
                    return false;
            }

            return true;
        }

        public static PolyCurve? OffsetSafe(this PolyCurve curve, double distance, Vector normal)
        {
            if (curve.Curves.Count == 1 && curve.Curves[0].IIsLinear())
            {
                var firstCurve = curve.Curves[0];
                Line line = Create.Line(firstCurve.IStartPoint(), firstCurve.IEndPoint());
                Line offsetLine = line.Offset(distance, normal);
                var result = Create.PolyCurve(((ICurve)offsetLine).ObjectToList());

                return result;
            }
            else if(curve.Curves.Count == 1 && curve.Curves[0] is Polygon)
            {
                var offset = (curve.Curves[0] as Polygon).IOffset(distance, normal);
                return Create.PolyCurve(offset.ObjectToList());
            }
            else
            {
                return curve.Offset(distance, normal);
            }
        }

        public static List<PolyCurve> OffsetBothSides(this PolyCurve curve, double distance, Vector normal)
        {
            PolyCurve curve1 = curve.OffsetSafe(distance, normal);
            PolyCurve curve2 = curve.OffsetSafe(-distance, normal).Flip();
            
            if (curve.IsClosed())
                return new List<PolyCurve>() { curve1, curve2 };

            Line cap1 = Create.Line(curve1.EndPoint(), curve2.StartPoint());
            Line cap2 = Create.Line(curve2.EndPoint(), curve1.StartPoint());

            PolyCurve joinedCrv = new PolyCurve();
            joinedCrv.Curves.AddRange(curve1.Curves);
            joinedCrv.Curves.Add(cap1);
            joinedCrv.Curves.AddRange(curve2.Curves);
            joinedCrv.Curves.Add(cap2);

            return joinedCrv.ObjectToList();
        }

        public static bool IsContainingAny(this PolyCurve curve, List<Point> points, bool acceptOnEdge = true, double tolerance = 1E-06)
        {
            foreach (Point point in points)
            {
                if (curve.IsContaining(point.ObjectToList(), acceptOnEdge, tolerance))
                    return true;
            }

            return false;
        }
    }
}
