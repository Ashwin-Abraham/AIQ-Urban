using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.GeometryOperations;
using AIQ.Urban.Core.Models.GeometryElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.GeometryOperations;

[RhinoTestFixture]
public class DelaunayRefinerTests
{
    [Test]
    [TestCase("SimpleSquare")]
    [TestCase("LShape")]
    [TestCase("Jagged")]
    [TestCase("UShape")]
    [TestCase("NonDelaunayQuad")]
    [TestCase("EmptyMesh")]
    [TestCase("NullMesh")]
    public void Refine_ReturnsValidDelaunayMesh(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);
        var inputMesh = testData.InputFactory();
        if (inputMesh is null)
        {
            Assert.That(() => DelaunayRefiner.Refine(null!), Throws.ArgumentNullException);
            return;
        }
        var result = DelaunayRefiner.Refine(inputMesh);
        Assert.That(result, Is.Not.Null);
        if (testData.ExpectsEmpty)
        {
            Assert.That(result.Faces.Count, Is.Zero);
            return;
        }
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Faces.Count, Is.EqualTo(testData.ExpectedTriangleCount));
        Assert.That(result.Faces.All(f => f.IsTriangle), Is.True);
        var inputArea = AreaMassProperties.Compute(inputMesh).Area;
        var resultArea = AreaMassProperties.Compute(result).Area;
        Assert.That(resultArea, Is.EqualTo(inputArea).Within(1e-4));
        if (testData.ExpectedAverageArea.HasValue)
        {
            var avgArea = resultArea / result.Faces.Count;
            Assert.That(avgArea, Is.EqualTo(testData.ExpectedAverageArea.Value).Within(1e-2));
        }
    }

    private record DelaunayTestData(
        string CaseName,
        Func<Mesh> InputFactory,
        int ExpectedTriangleCount,
        double? ExpectedAverageArea = null,
        bool ExpectsEmpty = false);

    private static readonly List<DelaunayTestData> _testData = BuildTestData();

    private static List<DelaunayTestData> BuildTestData()
    {
        return
        [
            new(CaseName: "SimpleSquare", InputFactory: MakeSquare, ExpectedTriangleCount: 2, ExpectedAverageArea: 50),
            new(CaseName: "LShape", InputFactory: MakeLShape, ExpectedTriangleCount: 4, ExpectedAverageArea: 18.75),
            new(CaseName: "Jagged", InputFactory: MakeJaggedShape, ExpectedTriangleCount: 9, ExpectedAverageArea: 11.72),
            new(CaseName: "UShape", InputFactory: MakeUShape, ExpectedTriangleCount: 6, ExpectedAverageArea: 12),
            new(CaseName: "NonDelaunayQuad", InputFactory: MakeNonDelaunayQuad, ExpectedTriangleCount: 2, ExpectedAverageArea: 42.75),
            new(CaseName: "EmptyMesh", InputFactory: MakeEmptyMesh, ExpectedTriangleCount: 0, ExpectsEmpty: true),
            new(CaseName: "NullMesh", InputFactory: () => null!, ExpectedTriangleCount: 0),
        ];
    }

    private static Mesh MakeSquare()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        var profile = new Profile2D(brep, poly, []);
        return profile.Triangulate();
    }

    private static Mesh MakeLShape()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 5, 0), new Point3d(5, 5, 0),
            new Point3d(5, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        var profile = new Profile2D(brep, poly, []);
        return profile.Triangulate();
    }

    private static Mesh MakeJaggedShape()
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
        var profile = new Profile2D(brep, poly, []);
        return profile.Triangulate();
    }

    private static Mesh MakeUShape()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(7, 10, 0),
            new Point3d(7, 3, 0), new Point3d(3, 3, 0),
            new Point3d(3, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        var profile = new Profile2D(brep, poly, []);
        return profile.Triangulate();
    }

    private static Mesh MakeNonDelaunayQuad()
    {
        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);    // 0 - A
        mesh.Vertices.Add(10, 0, 0);   // 1 - B
        mesh.Vertices.Add(9, 10, 0);   // 2 - C
        mesh.Vertices.Add(1, 9, 0);    // 3 - D
        // Wrong diagonal A-C, should be B-D
        mesh.Faces.AddFace(0, 1, 2, 2); // T1: (A, B, C)
        mesh.Faces.AddFace(0, 2, 3, 3); // T2: (A, C, D)
        return mesh;
    }

    private static Mesh MakeEmptyMesh()
    {
        return new Mesh();
    }
}
