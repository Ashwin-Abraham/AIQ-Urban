using BH.Engine.Geometry;
using BH.oM.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.Core.Extensions
{
    public static class VectorExtensions
    {
        public static Vector Unitized(this Vector v)
        {
            double length = v.Length();
            if (length == 0) return Create.Vector();

            double x = v.X / length;
            double y = v.Y / length;
            double z = v.Z / length;

            return Create.Vector(x, y, z);
        }
    }
}
