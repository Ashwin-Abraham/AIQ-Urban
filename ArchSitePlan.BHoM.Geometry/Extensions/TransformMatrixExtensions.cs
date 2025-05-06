using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.oM.Geometry;

namespace ArchSitePlan.BHoM.Geometry.Extensions;
public static class TransformMatrixExtensions
{
    public static TransformMatrix GenerateRotationAboutZ(double rotationDegrees)
    {
        // Convert degrees to radians
        double rotation = rotationDegrees * Math.PI / 180.0;

        double cos = Math.Cos(rotation);
        double sin = Math.Sin(rotation);

        double[,] matrix = new double[4, 4];

        // Rotation around Z-axis
        matrix[0, 0] = cos;
        matrix[0, 1] = -sin;
        matrix[0, 2] = 0;
        matrix[0, 3] = 0;

        matrix[1, 0] = sin;
        matrix[1, 1] = cos;
        matrix[1, 2] = 0;
        matrix[1, 3] = 0;

        matrix[2, 0] = 0;
        matrix[2, 1] = 0;
        matrix[2, 2] = 1;
        matrix[2, 3] = 0;

        matrix[3, 0] = 0;
        matrix[3, 1] = 0;
        matrix[3, 2] = 0;
        matrix[3, 3] = 1;

        return new TransformMatrix { Matrix = matrix };
    }
}
