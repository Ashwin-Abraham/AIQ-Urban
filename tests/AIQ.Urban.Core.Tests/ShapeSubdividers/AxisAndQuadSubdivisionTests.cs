//using System;
//using AIQ.Urban.Core.Models.GeometryElements;
//using AIQ.Urban.Core.Models.ShapeSubdividers;
//using NUnit.Framework;
//using Rhino.Geometry;
//using Rhino.Testing.Fixtures;

//namespace AIQ.Urban.Core.Tests.ShapeSubdividers;

//[RhinoTestFixture]
//public class AxisAndQuadSubdivisionTests
//{
//    [Test]
//    public void Run_ThrowsNotImplementedException()
//    {
//        // Arrange
//        var profile = Profile2D.Create(
//            Polygon2D.Create([(0, 0), (10, 0), (10, 10), (0, 10)]), [])!;
//        var subdivider = new AxisAndQuadSubdivision(Axis.X);

//        // Act & Assert
//        Assert.That(() => subdivider.Run(profile), Throws.TypeOf<NotImplementedException>());
//    }
//}
