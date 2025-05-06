using AIQ.Primitives.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Extensions;

public static class VectorExtensions
{
    public static Vector Unitized(this Vector v)
    {
        double length = v.Length();
        if (length.AlmostZero()) return Create.Vector();

        double x = v.X / length;
        double y = v.Y / length;
        double z = v.Z / length;

        return Create.Vector(x, y, z);
    }

    public static TransformMatrix ToTransform(this Vector v)
    {
        double[,] matrix = new double[4, 4];

        // Identity matrix
        matrix[0, 0] = 1;
        matrix[1, 1] = 1;
        matrix[2, 2] = 1;
        matrix[3, 3] = 1;

        // Apply vector
        matrix[0, 3] = v.X;
        matrix[1, 3] = v.Y;
        matrix[2, 3] = v.Z;
        matrix[3, 3] = 1;

        var transform = new TransformMatrix() { Matrix = matrix };
        return transform;
    }
}
