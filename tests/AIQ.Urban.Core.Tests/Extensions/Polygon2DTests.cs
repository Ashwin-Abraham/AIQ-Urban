//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using AIQ.Urban.Core.Models.GeometryElements;
//using AIQ.Urban.DebugViz;
//using NUnit.Framework;
//using Rhino;
//using Rhino.Geometry;
//using Rhino.Testing.Fixtures;

//namespace AIQ.Urban.Core.Tests.Extensions;

//[RhinoTestFixture]
//public class Polygon2DTests
//{
//    [Test]
//    [TestCase("SquareClosed")]
//    [TestCase("TriangleAutoClosed")]
//    [TestCase("PentagonExplicitClosed")]
//    public void Create_ValidCoords_ReturnsPolygon2D(string testName)
//    {
//        // Arrange
//        var testData = _validTestData.Single(x => x.CaseName == testName);

//        // Act
//        var result = Polygon2D.Create(testData.Input);

//        // Assert
//        Assert.That(result, Is.Not.Null);
//        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
//        Assert.That(result.Geometry.IsClosed, Is.True);
//        Assert.That(result.Geometry.IsPlanar(), Is.True);
//        var polyline = result.Geometry as PolylineCurve;
//        Assert.That(polyline, Is.Not.Null);
//        Assert.That(polyline!.PointCount, Is.EqualTo(testData.ExpectedPointCount));
//    }

//    [Test]
//    public void Create_NullInput_Throws()
//    {
//        // Arrange, Act & Assert
//        var ex = Assert.Throws<ArgumentNullException>(() =>
//            Polygon2D.Create(null!));
//        Assert.That(ex!.Message, Does.Contain("Value cannot be null"));
//    }

//    [Test]
//    [TestCase("TwoPoints")]
//    [TestCase("EmptyArray")]
//    public void Create_InvalidInput_Throws(string testName)
//    {
//        // Arrange
//        var testData = _invalidTestData.Single(x => x.CaseName == testName);

//        // Act & Assert
//        var ex = Assert.Throws<ArgumentException>(() =>
//            Polygon2D.Create(testData.Input));
//        Assert.That(ex!.Message, Does.Contain(testData.ExpectedMessageFragment));
//    }

//    private record TestData(
//        string CaseName,
//        List<(double X, double Y)> Input,
//        int ExpectedPointCount);

//    private static readonly List<TestData> _validTestData =
//    [
//        new("SquareClosed", [(0, 0), (10, 0), (10, 10), (0, 10), (0, 0)], 5),
//        new("TriangleAutoClosed", [(0, 0), (10, 0), (5, 10)], 4),
//        new("PentagonExplicitClosed", [(0, 0), (5, 0), (6, 5), (3, 8), (-2, 4), (0, 0)], 6),
//    ];

//    private record InvalidTestData(
//        string CaseName,
//        List<(double X, double Y)> Input,
//        string ExpectedMessageFragment);

//    private static readonly List<InvalidTestData> _invalidTestData =
//    [
//        new("TwoPoints", [(0, 0), (10, 0)], "required"),
//        new("EmptyArray", [], "required"),
//    ];

//    [Test]
//    public void Constructor_StoresIndependentCopy_NotAffectedByDocDeletion()
//    {
//        // Arrange
//        using var doc = RhinoDoc.CreateHeadless(null);
//        var polyline = new PolylineCurve([
//            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
//            new Point3d(10, 10, 0), new Point3d(0, 10, 0),
//            new Point3d(0, 0, 0),
//        ]);
//        var id = doc.Objects.AddCurve(polyline);
//        var obj = doc.Objects.Find(id);
//        var curve = obj!.Geometry as Curve;

//        // Act
//        var sut = new Polygon2D(curve!);
//        doc.Objects.Delete(obj, true);

//        // Assert
//        Assert.That(sut.Geometry.IsValid, Is.True);
//        Assert.That(sut.Geometry.IsClosed, Is.True);
//    }

//    [Test]
//    public void Constructor_StoresIndependentCopy_NotAffectedByDocModification()
//    {
//        // Arrange
//        using var doc = RhinoDoc.CreateHeadless(null);
//        var polyline = new PolylineCurve([
//            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
//            new Point3d(10, 10, 0), new Point3d(0, 10, 0),
//            new Point3d(0, 0, 0),
//        ]);
//        var id = doc.Objects.AddCurve(polyline);
//        var obj = doc.Objects.Find(id);
//        var curve = obj!.Geometry as Curve;

//        // Act
//        var sut = new Polygon2D(curve!);
//        doc.Objects.AddLine(new Line(new Point3d(99, 0, 0), new Point3d(99, 10, 0)));

//        // Assert
//        Assert.That(sut.Geometry.IsValid, Is.True);
//        Assert.That(sut.Geometry.IsClosed, Is.True);
//    }

//    [Test]
//    public void Rectangle_ReturnsCorrectPolygon()
//    {
//        // Arrange
//        var min = new Point2d(2, 3);
//        var max = new Point2d(8, 7);

//        // Act
//        var result = Polygon2D.Rectangle(min, max);

//        // Visualise
//#if DEBUG
//        VisualisePolygon(result);
//#endif

//        // Assert
//        var points = result.Points();
//        Assert.That(points, Has.Count.EqualTo(5));
//        Assert.That(points[0], Is.EqualTo(new Point3d(2, 3, 0)));
//        Assert.That(points[1], Is.EqualTo(new Point3d(8, 3, 0)));
//        Assert.That(points[2], Is.EqualTo(new Point3d(8, 7, 0)));
//        Assert.That(points[3], Is.EqualTo(new Point3d(2, 7, 0)));
//        Assert.That(points[4], Is.EqualTo(new Point3d(2, 3, 0)));

//        Assert.That(result.Geometry.IsClosed, Is.True);
//        Assert.That(result.Geometry.IsPlanar(), Is.True);
//    }

//    [Test]
//    public void Points_ReturnsCorrectVertices()
//    {
//        // Arrange
//        var poly = Polygon2D.Create([(0, 0), (10, 0), (10, 10), (0, 10)]);

//        // Act
//        var points = poly.Points();

//        // Assert
//        Assert.That(points, Has.Count.EqualTo(5));
//        Assert.That(points[0], Is.EqualTo(new Point3d(0, 0, 0)));
//        Assert.That(points[2], Is.EqualTo(new Point3d(10, 10, 0)));
//    }

//    [Test]
//    public void ToBrep_ReturnsPlanarBrep()
//    {
//        // Arrange
//        var poly = Polygon2D.Create([(0, 0), (10, 0), (10, 10), (0, 10)]);

//        // Act
//        var brep = poly.ToBrep();

//        // Visualise
//#if DEBUG
//        VisualiseBrep(poly, brep);
//#endif

//        // Assert
//        Assert.That(brep, Is.Not.Null);
//        Assert.That(brep.Faces.Count, Is.EqualTo(1));
//        var area = AreaMassProperties.Compute(brep).Area;
//        Assert.That(area, Is.EqualTo(100).Within(1e-4));
//    }

//#if DEBUG
//    private static void VisualisePolygon(Polygon2D polygon)
//    {
//        var doc = RhinoDoc.CreateHeadless(null);
//        var attribs = RhinoHelper.MakeAttributes(doc, "Polygon", Color.Blue);
//        doc.Objects.AddCurve(polygon.Geometry, attribs);
//        Visualizer.View(doc);
//    }

//    private static void VisualiseBrep(Polygon2D polygon, Brep brep)
//    {
//        var doc = RhinoDoc.CreateHeadless(null);
//        var outlineAttribs = RhinoHelper.MakeAttributes(doc, "Outline", Color.Black);
//        var brepAttribs = RhinoHelper.MakeAttributes(doc, "Brep", Color.FromArgb(125, Color.LightBlue));
//        doc.Objects.AddCurve(polygon.Geometry, outlineAttribs);
//        doc.Objects.AddBrep(brep, brepAttribs);
//        Visualizer.View(doc);
//    }
//#endif
//}
