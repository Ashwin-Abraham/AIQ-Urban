using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.DebugViz;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.GeometryOperations;

[RhinoTestFixture]
public class SweepLineTriangulatorTests
{
    [Test]
    [TestCase("SimpleSquare")]
    [TestCase("RectangleWithHole")]
    [TestCase("LShape")]
    [TestCase("Jagged")]
    [TestCase("UShape")]
    [TestCase("8Shape")]
    public void Triangulate_ProducesValidMesh(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        var profile = testData.Input;

        // Act
        PreviewHub.ResetDoc();
        var mesh = profile.Triangulate2();

        // Visualise
#if DEBUG
        mesh.PreviewViaHub();
#endif

        // Assert


        //// Assert
        //Assert.That(mesh.IsValid, Is.True);
        //Assert.That(mesh.Vertices.Count, Is.GreaterThan(0));
        //Assert.That(mesh.Faces.Count, Is.GreaterThan(0));
        //Assert.That(mesh.Faces.All(f => f.IsTriangle), Is.True);

        //Assert.That(mesh.Faces.Count, Is.EqualTo(testData.ExpectedTriangleCount));

        //var meshArea = AreaMassProperties.Compute(mesh).Area;
        //var profileArea = AreaMassProperties.Compute(profile.Geometry).Area;
        //Assert.That(meshArea, Is.EqualTo(profileArea).Within(1e-4));

        //var avgArea = meshArea / mesh.Faces.Count;
        //Assert.That(avgArea, Is.EqualTo(testData.ExpectedAverageArea).Within(1e-2));
    }

    [Test]
    public void Triangulate_DegenerateProfile_ReturnsEmptyMesh()
    {
        // Arrange: a profile with near-zero area outline (thin sliver triangle)
        // Area = 0.5 * 1 * 5e-6 = 2.5e-6, below triangulation tolerance 1e-5
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0),
            new Point3d(1, 0, 0),
            new Point3d(0.5, 5e-6, 0),
            new Point3d(0, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        var profile = new Profile2D(poly, []);

        // Act
        var mesh = profile.Triangulate();

        // Assert
        Assert.That(mesh.Vertices.Count, Is.Zero);
        Assert.That(mesh.Faces.Count, Is.Zero);
    }

    private record TriangulationTestData(string CaseName, Profile2D Input, int ExpectedTriangleCount, double ExpectedAverageArea);

    private static readonly List<TriangulationTestData> _testData = BuildTestData();

    private static List<TriangulationTestData> BuildTestData()
    {
        return
        [
            new(CaseName: "SimpleSquare",
                Input: MakeSquare(), 
                ExpectedTriangleCount: 2,
                ExpectedAverageArea: 50),

            new(CaseName: "RectangleWithHole",
                Input: MakeRectangleWithHole(),
                ExpectedTriangleCount: 8, 
                ExpectedAverageArea: 17.5),

            new(CaseName: "LShape", 
                Input: MakeLShape(), 
                ExpectedTriangleCount: 4, 
                ExpectedAverageArea: 18.75),

            new(CaseName: "Jagged",
                Input: MakeJaggedShape(), 
                ExpectedTriangleCount: 9,
                ExpectedAverageArea: 11.72),

            new(CaseName: "UShape", 
                Input: MakeUShape(),
                ExpectedTriangleCount: 6, 
                ExpectedAverageArea: 12),

            new(CaseName: "8Shape", 
                Input: Make8Shape(),
                ExpectedTriangleCount: 6,
                ExpectedAverageArea: 12),
        ];
    }

    private static Profile2D Make8Shape()
    {
        var profile = new Profile2D(
            outline: Polygon2D.Create(
            [
                (0, 0),
                (5, 0),
                (7, 2),
                (9, 0),
                (14, 0),
                (14, 6),
                (9, 6),
                (7, 4),
                (5, 6),
                (0, 6),
            ]),
            holes:
            [
                Polygon2D.Create(
                [
                    (2, 2),
                    (4, 2),
                    (4, 4),
                    (2, 4),
                    (2, 2),
                ]),
                Polygon2D.Create(
                [
                    (10, 2),
                    (12, 2),
                    (12, 4),
                    (10, 4),
                    (10, 2),
                ])
            ]);

        return profile;
    }

    private static Profile2D MakeSquare()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        return new Profile2D(poly, []);
    }

    private static Profile2D MakeRectangleWithHole()
    {
        var outer = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(20, 0, 0),
            new Point3d(20, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var hole = new Polygon2D(new PolylineCurve([
            new Point3d(5, 2, 0), new Point3d(15, 2, 0),
            new Point3d(15, 8, 0), new Point3d(5, 8, 0),
            new Point3d(5, 2, 0),
        ]));
        var brep = Brep.CreatePlanarBreps([outer.Geometry, hole.Geometry], 1e-6)[0];
        return new Profile2D(outer, [hole]);
    }

    private static Profile2D MakeLShape()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 5, 0), new Point3d(5, 5, 0),
            new Point3d(5, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        return new Profile2D(poly, []);
    }

    private static Profile2D MakeJaggedShape()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 2, 0),
            new Point3d(10, 2, 0),
            new Point3d(9, 6, 0),
            new Point3d(13, 12, 0),
            new Point3d(15, 8, 0),
            new Point3d(20, 13, 0),
            new Point3d(18, 18, 0),
            new Point3d(13, 16, 0),
            new Point3d(9, 21, 0),
            new Point3d(11, 14, 0),
            new Point3d(4, 4, 0),
            new Point3d(0, 2, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        return new Profile2D(poly, []);
    }

    private static Profile2D MakeUShape()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(7, 10, 0),
            new Point3d(7, 3, 0), new Point3d(3, 3, 0),
            new Point3d(3, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        return new Profile2D(poly, []);
    }

#if DEBUG
    private static void PreviewMesh(Profile2D profile, Mesh mesh)
    {
        var doc = RhinoDoc.CreateHeadless(null);
        var outlineAttribs = RhinoHelper.MakeAttributes(doc, "Outline", System.Drawing.Color.Black);
        doc.Objects.AddCurve(profile.Outline.Geometry, outlineAttribs);

        var edgeCount = mesh.TopologyEdges.Count;

        for (int i = 0; i < edgeCount; i++)
            doc.Objects.AddCurve(mesh.TopologyEdges.EdgeLine(i).ToNurbsCurve(), outlineAttribs);

        var meshAttribs = RhinoHelper.MakeAttributes(doc, "Mesh", Color.FromArgb(125, Color.PaleVioletRed));
        doc.Objects.AddMesh(mesh, meshAttribs);
        Visualizer.View(doc);
    }
#endif
}