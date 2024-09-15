using BH.Engine.Geometry;
using BH.oM.Geometry;
using BH.oM.Geometry.CoordinateSystem;

namespace ArchSitePlan.Core.Extensions
{
    public static class PolylineExtensions
    {
        public static List<Line> Segments(this Polyline polyline)
        {
            List<Line> segments = new();
            
            for(int i =0; i< polyline.ControlPoints.Count - 1; i++)
                segments.Add(Create.Line(polyline.ControlPoints[i], polyline.ControlPoints[i+1]));

            return segments;
        }

        public static PolyCurve Fillet(this Polyline polyline, double radius)
        {
            var polyCurveSegments = new List<ICurve>();

            var segments = polyline.Segments();
            for(int i = 0; i< segments.Count -1; i++)
            {
                var filletted = segments[i].ArcBetweenLinesThatMeet(segments[i+1], radius);
                polyCurveSegments.AddRange(filletted.Curves);

                //if (segments[i].Length() < radius || segments[i + 1].Length() < radius)
                //{
                    
                //    continue;
                //}

                //var bisectorDirection = (segments[i].Direction().Reverse() + segments[i + 1].Direction()) / 2;
                //var center = segments[i].End + (bisectorDirection * radius * 2);
                //var middle = center - bisectorDirection * radius;

                //segments[i] = segments[i].Extend(0, -radius);
                //segments[i + 1] = segments[i+1].Extend(-radius, 0);
                //var filletArc = Create.ArcByCentre(center, segments[i].End, segments[i+1].Start);

                //polyCurveSegments.Add(segments[i]);
                //polyCurveSegments.Add(filletArc);
            }

            //polyCurveSegments.Add(segments.Last());

            return Create.PolyCurve(polyCurveSegments);
        }
    }
}
