using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIQ.Primitives.Extensions;
using ArchSitePlan.BHoM.Geometry.Extensions;
using BH.Engine.Geometry;
using BH.oM.Geometry;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchSitePlan.BHoM.Geometry.Models;
public class PlaneXYZ
{
    public Vector XAxis { get; private set; }
    public Vector YAxis { get; private set; }
    public Vector ZAxis { get; private set; }
    public Point Origin { get; set; }
    
    public PlaneXYZ()
    {
        Origin = Point.Origin;
        XAxis = Vector.XAxis;
        YAxis = Vector.YAxis;
        ZAxis = Vector.ZAxis;
    }

    public PlaneXYZ(Point origin, Vector xAxis, Vector yReference, bool flipY = false)
    {
        XAxis = xAxis.Unitized();

        ZAxis = XAxis.CrossProduct(yReference).Unitized();
       
        if(ZAxis.Length().AlmostZero())
            throw new Exception("Plane creation unsuccessful. Reference is parallel to primary axis.");

        YAxis = ZAxis.CrossProduct(XAxis).Unitized();

        if (flipY)
            YAxis = YAxis.Reverse();

        Origin = origin;
    }

    public TransformMatrix AsTransform()
    {
        var toPlane = new double[4, 4]
        {
        { XAxis.X, YAxis.X, ZAxis.X, Origin.X },
        { XAxis.Y, YAxis.Y, ZAxis.Y, Origin.Y },
        { XAxis.Z, YAxis.Z, ZAxis.Z, Origin.Z },
        { 0,       0,       0,       1 }
        };

        return new TransformMatrix() { Matrix = toPlane };
    }

    public TransformMatrix GetTransformTo(PlaneXYZ targetPlane)
    {
        // Build matrix from this plane to world
        var toOrigin = AsTransform().Invert();

        // Build matrix from target plane to world coordinates
        var toTarget = targetPlane.AsTransform();

        // Multiply world-to-target * toWorld to get the final transform
        return toTarget * toOrigin;
    }

    public PlaneXYZ RotatedAroundZAxis(double degrees)
    {
        return new PlaneXYZ()
        {
            Origin = Origin,
            XAxis = XAxis.Rotate(degrees.ToRadians(), ZAxis),
            YAxis = YAxis.Rotate(degrees.ToRadians(), ZAxis),
            ZAxis = ZAxis
        };
    }

    public PlaneXYZ Flipped(bool flipX = false, bool flipY = false, bool flipZ = false)
    {
        var newPlane = new PlaneXYZ();
        newPlane.Origin = Origin;
        newPlane.XAxis = flipX? XAxis: XAxis.Reverse();
        newPlane.YAxis = flipY? YAxis: YAxis.Reverse();
        newPlane.ZAxis = flipY? ZAxis: ZAxis.Reverse();
        return newPlane;
    }
}
