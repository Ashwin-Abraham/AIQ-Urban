using Elements;
using Elements.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Extensions
{
    public static class ArcExtensions
    {
        /// <summary>
        /// Forms an arc by drawing from a to b to c
        /// </summary>
        public static Arc FromThreePoints(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 center = (a + b + c) / 3;

            Circle circle = new Circle(center, center.DistanceTo(a));
            circle.ParameterAt(a, out double paramA);
            circle.ParameterAt(c, out double paramC);

            return new Arc(circle, paramA, paramC);
        }

        public static IndexedPolycurve? Offset(this Arc arc, 
            double offset, 
            double minRadius = Vector3.EPSILON)
        {
            if (offset.ApproximatelyEquals(0)) return null;

            bool reverse = offset < 0;
            offset = Math.Abs(offset);

            double radius = arc.Radius;

            Arc outerArc = new Arc(arc.Center, radius + offset, arc.StartAngle, arc.EndAngle);
            Arc innerArc;
            
            if(radius < (offset + minRadius))
            {
                innerArc = new Arc(arc.Center, minRadius, arc.StartAngle, arc.EndAngle);
            }
            else
            {
                innerArc = new Arc(arc.Center, radius-offset, arc.StartAngle, arc.EndAngle);
            }

            Line capEnd = new Line(outerArc.End, innerArc.End);
            Line capStart = new Line(innerArc.Start, outerArc.Start);

            List<BoundedCurve> crvs = new List<BoundedCurve>()
            {
                outerArc,
                capEnd,
                innerArc,
                capStart
            };

            return new IndexedPolycurve(crvs);
        }
    }
}
