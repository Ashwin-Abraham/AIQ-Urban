using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.DebugModel
{
    public static class HyparDebugExtensions
    {
        public static Polygon ToPolygon(this Triangle triangle)
        {
            List<Vector3> points = triangle.Vertices.Select(v => v.Position).ToList();
            return new Polygon(points);
        }
        public static Polygon ToPolygonalCircle(this Circle circle, int divisions = 6)
        {
            return circle.ToPolygon(divisions);
            List<Vector3> points = new List<Vector3>();

            for (int i = 1; i < divisions; i++)
            {
                Vector3 newPt = circle.PointAt(1 / ((double)divisions * i));
                points.Add(newPt);
            }

            return new Polygon(points);
        }

        public static Polygon CreateRectangle(Transform center, double xSize, double ySize)
        {
            Vector3 v1 = center.Origin + center.XAxis * xSize + center.YAxis * ySize;
            Vector3 v2 = center.Origin + center.XAxis * xSize - center.YAxis * ySize;
            Vector3 v3 = center.Origin - center.XAxis * xSize - center.YAxis * ySize;
            Vector3 v4 = center.Origin - center.XAxis * xSize + center.YAxis * ySize;

            Polygon rectangle = new Polygon(v1, v2, v3, v4);
            return rectangle;
        }

        public static IEnumerable<Polygon> CreateRectangles(List<Transform> centers,
                                                double xSize,
                                                double ySize)
        {
            List<Polygon> rectangles = new List<Polygon>();

            for (int i = 0; i < centers.Count; i++)
            {
                rectangles.Add(CreateRectangle(centers[i],
                                                     xSize,
                                                     ySize));
            }

            return rectangles;
        }
    }
}
