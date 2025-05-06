using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchSitePlan.BHoM.Geometry.Extensions;
using ArchSitePlan.BHoM.Geometry.Models;
using ArchSitePlan.DebugModel;
using BH.Engine.Geometry;
using BH.oM.Geometry;

namespace ArchSitePlan.Test.BHoM.Geometry.Models;
public class PlaneXYZTests
{
    [Fact]
    public void CanCreate()
    {
        // Arrange
        var yRef = Create.Vector(0, 1);
        var xAxis = Create.Vector(1, 0);

        // Act
        var plane = new PlaneXYZ(
            origin: Create.Point(),
            xAxis: xAxis,
            yReference: yRef);

        // Assert
        Assert.Equal(plane.ZAxis, Vector.ZAxis);
    }

    [Fact]
    public void CanCreate_OddYAxisInXYPlane()
    {
        // Arrange
        var yRef = Create.Vector(1, 1);
        var xAxis = Create.Vector(1, 0);

        // Act
        var plane = new PlaneXYZ(
            origin: Create.Point(),
            xAxis: xAxis,
            yReference: yRef);

        // Assert
        Assert.Equal(plane.XAxis, Vector.XAxis);
        Assert.Equal(plane.YAxis, Vector.YAxis);
        Assert.Equal(plane.ZAxis, Vector.ZAxis);
    }

    [Fact]
    public void CanCreate_NegY()
    {
        // Arrange
        var yRef = Create.Vector(-1, 1);
        var xAxis = Create.Vector(1, 0);

        // Act
        var plane = new PlaneXYZ(
            origin: Create.Point(),
            xAxis: xAxis,
            yReference: yRef,
            true);

        // Assert
        Assert.Equal(plane.ZAxis, Vector.ZAxis);
        Assert.Equal(plane.YAxis, Vector.YAxis.Reverse());
    }

    [Fact]
    public void CanCreate_NegZNegY()
    {
        // Arrange
        var yRef = Create.Vector(-1, -1);
        var xAxis = Create.Vector(1, 0);

        // Act
        var plane = new PlaneXYZ(
            origin: Create.Point(),
            xAxis: xAxis,
            yReference: yRef,
            false);

        // Assert
        Assert.Equal(plane.XAxis, Vector.XAxis);
        Assert.Equal(plane.ZAxis, Vector.ZAxis.Reverse());
        Assert.Equal(plane.YAxis, Vector.YAxis.Reverse());
    }

}
