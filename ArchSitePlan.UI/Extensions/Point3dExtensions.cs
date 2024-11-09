using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.UI.Extensions
{
    public static class Point3dExtensions
    {
        public static Point3d AveragePoint(this IEnumerable<Point3d> points)
        {
            Point3d sum = new();
            
            foreach(var pt in points)
            {
                sum.X += pt.X;
                sum.Y += pt.Y;
                sum.Z += pt.Z;
            }

            sum.X /= points.Count();
            return sum;
        }
    }
}
