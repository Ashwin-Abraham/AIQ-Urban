using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Extensions;

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
