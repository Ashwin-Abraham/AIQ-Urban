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
            }

            return Create.PolyCurve(polyCurveSegments);
        }
    }
}
