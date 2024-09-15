using BH.Engine.Geometry;
using BH.oM.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Models
{
    enum RoadEnd { Start, End }
    public class RoadNetwork
    {
        public List<Road> Roads { get; set; }
        public List<PlanarSurface> CombinedRoWs { get; set; }
        public RoadNetwork(List<Road> roads)
        {
            Roads = roads;
            ResetROWs();
        }

        private void ResetROWs()
        {
            Roads.ForEach(r => r.ResetRoW());
        }

        private void CalculateRoWInNetwork()
        {
            for (int i = 0; i < Roads.Count; i++)
            {
                var road = Roads[i];
                double maxExtension = road.Width / 2;

                // EXTEND START
                List<Road> connectedAtStart = GetAdjacentRoads(road, RoadEnd.Start);
                Point startPt = road.Centerline.StartPoint();

                List<ICurve> boundingGeometry = connectedAtStart
                    .Select(r => r.RoW == null ? r.Centerline : r.RoW.ExternalBoundary)
                    .ToList();

                PolyCurve offset1 = road.Centerline.Offset(road.Width / 2, Vector.ZAxis);

                var intersectionPts = IntersectsICurves(offset1, boundingGeometry);
                if (intersectionPts.Any())
                {
                    double extensionDistance = intersectionPts.Min(pt => pt.Distance(startPt));
                    offset1 = offset1.Extend(extensionDistance);
                }

                // EXTEND START OTHER SIDE

                // EXTEND END
                List<Road> connectedAtEnd = GetAdjacentRoads(road, RoadEnd.End);
                Point endPt = road.Centerline.EndPoint();

                boundingGeometry = connectedAtEnd
                    .Select(r => r.RoW == null ? r.Centerline : r.RoW.ExternalBoundary)
                    .ToList();

                PolyCurve offset2 = road.Centerline.Offset(-road.Width / 2, Vector.ZAxis);

                intersectionPts = IntersectsICurves(offset2, boundingGeometry);
                if (intersectionPts.Any())
                {
                    double extensionDistance = intersectionPts.Min(pt => pt.Distance(endPt));
                    offset2 = offset2.Extend(extensionDistance);
                }
                // EXTEND END OTHER WAY

                // CREATE BOUNDARY
                var boundary = new PlanarSurface(
                    externalBoundary: new Polygon( new List<Point>
                    {
                        road.Centerline.StartPoint(),
                        offset1.StartPoint(),
                        offset1.EndPoint(),
                        road.Centerline.EndPoint(),
                        offset2.EndPoint(),
                        offset2.StartPoint()
                    }),
                    internalBoundaries: new());
            }
        }

        public static List<Point> IntersectsICurves(
            PolyCurve curve, 
            IEnumerable<ICurve> curveSet)
        {
            var intersectionPoints = new List<Point>();

            foreach (var c in curveSet)
            {
                intersectionPoints.AddRange(curve.ICurveIntersections(c));
            }

            return intersectionPoints;
        }

        private List<Road> GetAdjacentRoads(Road road, RoadEnd start, double tolerance = 1E-05 )
        {
            var connectionPt = start switch
            {
                RoadEnd.Start => road.Centerline.StartPoint(),
                RoadEnd.End => road.Centerline.EndPoint()
            };

            var connectedRoads = Roads.FindAll(r => 
            r.Centerline.StartPoint().Distance(connectionPt) < tolerance
            || r.Centerline.EndPoint().Distance(connectionPt) < tolerance);

            return connectedRoads;
        }
    }
}
