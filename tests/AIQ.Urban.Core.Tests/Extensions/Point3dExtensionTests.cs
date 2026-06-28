//using System;
//using System.Linq;
//using AIQ.Urban.Core.Extensions;
//using NUnit.Framework;
//using Rhino;
//using Rhino.Geometry;
//using Rhino.Testing.Fixtures;

//namespace AIQ.Urban.Core.Tests.Extensions;

//[RhinoTestFixture]
//public class Point3dExtensionTests
//{
//    [Test]
//    [TestCase("Origin", 0, 0, 0, 0, 0)]
//    [TestCase("Positive", 3.5, 7.2, 0, 3.5, 7.2)]
//    [TestCase("Negative", -2.5, -4.1, 0, -2.5, -4.1)]
//    public void ToPoint2d_DropsZ(string testName, double x, double y, double z, double expectedX, double expectedY)
//    {
//        // Arrange
//        var point = new Point3d(x, y, z);

//        // Act
//        var result = point.ToPoint2d();

//        // Assert
//        Assert.That(result.X, Is.EqualTo(expectedX).Within(1e-6));
//        Assert.That(result.Y, Is.EqualTo(expectedY).Within(1e-6));
//    }

//    [Test]
//    [TestCase("Origin", 0, 0, 0, 0, 0, 0)]
//    [TestCase("Positive", 3.5, 7.2, 9.1, 3.5, 7.2, 9.1)]
//    [TestCase("Negative", -2.5, -4.1, -6.3, -2.5, -4.1, -6.3)]
//    public void ToVector3d_ConvertsCorrectly(string testName, double x, double y, double z, double expectedX, double expectedY, double expectedZ)
//    {
//        // Arrange
//        var point = new Point3d(x, y, z);

//        // Act
//        var result = point.ToVector3d();

//        // Assert
//        Assert.That(result.X, Is.EqualTo(expectedX).Within(1e-6));
//        Assert.That(result.Y, Is.EqualTo(expectedY).Within(1e-6));
//        Assert.That(result.Z, Is.EqualTo(expectedZ).Within(1e-6));
//    }

//    [Test]
//    [TestCase("AlongX", 10, 0, 0, 0)]
//    [TestCase("AlongY", 0, 10, 0, 90)]
//    [TestCase("AlongNegativeX", -5, 0, 0, 180)]
//    [TestCase("AlongNegativeY", 0, -3, 0, 270)]
//    [TestCase("FirstQuadrant", 5, 5, 0, 45)]
//    [TestCase("SecondQuadrant", -3, 4, 0, 126.87)]
//    public void FlatAngleToX_ReturnsDegrees(string testName, double x, double y, double z, double expectedDegrees)
//    {
//        // Arrange
//        var point = new Point3d(x, y, z);

//        // Act
//        var result = point.FlatAngleToX();

//        // Assert
//        Assert.That(result, Is.EqualTo(expectedDegrees).Within(1e-2));
//    }
//}
