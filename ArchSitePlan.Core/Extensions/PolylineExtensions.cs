using BH.Engine.Geometry;
using BH.oM.Geometry;

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
    }
}
