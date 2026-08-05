using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.GeometryOperations.Triangulation;
using AIQ.Urban.Core.Models.GeometryElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.GeometryOperations;

[RhinoTestFixture]
public class TrapezoidalTriangulatorTests
{
    [Test]
    [TestCase("SimpleSquare")]
    [TestCase("RectangleWithHole")]
    [TestCase("LShape")]
    [TestCase("Jagged")]
    [TestCase("UShape")]
    [TestCase("8Shape")]
    public void Triangulate_ProducesValidTriangles(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        var profile = testData.Input;

        var triangles = TrapezoidalTriangulator.Triangulate(profile);

#if DEBUG
        // Preview
        var trianglePolygons = triangles.Select(x => new PolylineCurve([x.A, x.B, x.C, x.A])).ToList();
        PreviewHub.ResetDoc();
        profile.Outline.Geometry.Translate(0, 0, - 1);
        profile.Holes.ForEach(x => x.Geometry.Translate(0, 0, - 1));
        profile.AddToPreviewDoc(Color.Beige);
        trianglePolygons.PreviewViaHub(color: Color.Black);
#endif

        // Assert
        Assert.That(triangles, Has.Count.GreaterThan(0));

        foreach (var (a, b, c) in triangles)
        {
            var area = ComputeTriangleArea(a, b, c);
            Assert.That(area, Is.GreaterThan(1e-10), $"Degenerate triangle detected in {testName}");
        }

        var totalTriangleArea = triangles.Sum(t => ComputeTriangleArea(t.A, t.B, t.C));
        var profileArea = AreaMassProperties.Compute(profile.Geometry).Area;
        Assert.That(totalTriangleArea, Is.EqualTo(profileArea).Within(1e-4),
            $"Area mismatch in {testName}");
    }

    private static double ComputeTriangleArea(Point3d a, Point3d b, Point3d c)
    {
        var v1 = b - a;
        var v2 = c - a;
        var cross = v1.X * v2.Y - v1.Y * v2.X;
        return Math.Abs(cross) / 2.0;
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
            new(CaseName: "UShape", Input: MakeUShape()),
            new(CaseName: "8Shape", Input: Make8Shape()),
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
        return new Profile2D(poly, []);
    }
}
