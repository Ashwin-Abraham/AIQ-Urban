using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
    public void Triangulate_ProducesValidMesh(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        var profile = testData.Input;

        // Act
        var mesh = profile.Triangulate();

        // Visualise
#if DEBUG
        PreviewMesh(profile, mesh);
#endif

        // Assert
        Assert.That(mesh.IsValid, Is.True);
        Assert.That(mesh.Vertices.Count, Is.GreaterThan(0));
        Assert.That(mesh.Faces.Count, Is.GreaterThan(0));
        Assert.That(mesh.Faces.All(f => f.IsTriangle), Is.True);

        var meshArea = AreaMassProperties.Compute(mesh).Area;
        var profileArea = AreaMassProperties.Compute(profile.Geometry).Area;
        Assert.That(meshArea, Is.EqualTo(profileArea).Within(1e-4));
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
        var profile = new Profile2D(brep, poly, []);

        // Act
        var mesh = profile.Triangulate();

        // Assert
        Assert.That(mesh.Vertices.Count, Is.Zero);
        Assert.That(mesh.Faces.Count, Is.Zero);
    }

    private record TriangulationTestData(string CaseName, Profile2D Input);

    private static readonly List<TriangulationTestData> _testData = BuildTestData();

    private static List<TriangulationTestData> BuildTestData()
    {
        return
        [
            new(CaseName: "SimpleSquare", Input: MakeSquare()),
            new(CaseName: "RectangleWithHole", Input: MakeRectangleWithHole()),
            new(CaseName: "LShape", Input: MakeLShape()),
            new(CaseName: "Jagged", Input: MakeJaggedShape()),
        ];
    }

    private static Profile2D MakeSquare()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        return new Profile2D(brep, poly, []);
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
        return new Profile2D(brep, outer, [hole]);
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
        return new Profile2D(brep, poly, []);
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
        return new Profile2D(brep, poly, []);
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
