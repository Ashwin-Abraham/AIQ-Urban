using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.GeometryOperations;
using AIQ.Urban.Core.Models.GeometryElements;
using NUnit.Framework;
using Rhino;
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
    public void Refine_ProducesValidMesh(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        var mesh = testData.Input;

        // Act
        var result = DelaunayRefiner.Refine(mesh);

        // Assert
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Vertices.Count, Is.GreaterThan(0));
        Assert.That(result.Faces.Count, Is.GreaterThan(0));
        Assert.That(result.Faces.All(f => f.IsTriangle), Is.True);
        Assert.That(result.Faces.Count, Is.EqualTo(testData.ExpectedTriangleCount));

        var inputArea = AreaMassProperties.Compute(mesh).Area;
        var resultArea = AreaMassProperties.Compute(result).Area;
        Assert.That(resultArea, Is.EqualTo(inputArea).Within(1e-4));

        var avgArea = resultArea / result.Faces.Count;
        Assert.That(avgArea, Is.EqualTo(testData.ExpectedAverageArea).Within(1e-2));
    }

    [Test]
    public void Refine_NonDelaunayQuad_FlippedEdge()
    {
        // Arrange: a convex non-cyclic quad split with the "wrong" diagonal
        // Vertices: A(0,0), B(10,0), C(9,10), D(1,9)
        // Triangulated as ABC + ACD (diagonal AC = (0,0)-(9,10))
        var mesh = new Mesh();
        mesh.Vertices.Add(0f, 0f, 0f);  // 0: A
        mesh.Vertices.Add(10f, 0f, 0f); // 1: B
        mesh.Vertices.Add(9f, 10f, 0f); // 2: C
        mesh.Vertices.Add(1f, 9f, 0f);  // 3: D
        mesh.Faces.AddFace(0, 1, 2);    // ABC
        mesh.Faces.AddFace(0, 2, 3);    // ACD

        // Act
        var result = DelaunayRefiner.Refine(mesh);

        // Assert
        Assert.That(result.Faces.Count, Is.EqualTo(2));
        Assert.That(result.IsValid, Is.True);

        // The edge (0,0)-(9,10) (AC) should have been flipped
        // Result should use diagonal BD = (10,0)-(1,9) instead
        // Verify all 4 vertices are present in the result
        var face0 = result.Faces[0];
        var face1 = result.Faces[1];
        var vertsUsed = new[] { face0.A, face0.B, face0.C, face1.A, face1.B, face1.C }.Distinct().OrderBy(v => v).ToArray();
        Assert.That(vertsUsed, Is.EquivalentTo(new[] { 0, 1, 2, 3 }));

        // Verify the diagonal is BD (1-3), not AC (0-2)
        var allEdges = new HashSet<(int, int)>();
        void RecordEdge(int a, int b) { var e = a < b ? (a, b) : (b, a); allEdges.Add(e); }
        RecordEdge(face0.A, face0.B);
        RecordEdge(face0.B, face0.C);
        RecordEdge(face0.C, face0.A);
        RecordEdge(face1.A, face1.B);
        RecordEdge(face1.B, face1.C);
        RecordEdge(face1.C, face1.A);
        Assert.That(allEdges.Contains((0, 2)), Is.False, "Edge AC (0-2) should have been flipped");
        Assert.That(allEdges.Contains((1, 3)), Is.True, "Edge BD (1-3) should exist after flip");
    }

    [Test]
    public void Refine_EmptyMesh_ReturnsEmpty()
    {
        // Arrange
        var mesh = new Mesh();

        // Act
        var result = DelaunayRefiner.Refine(mesh);

        // Assert
        Assert.That(result.Vertices.Count, Is.Zero);
        Assert.That(result.Faces.Count, Is.Zero);
    }

    [Test]
    public void Refine_DegenerateTriangle_HandlesGracefully()
    {
        // Arrange: near-degenerate triangle
        var mesh = new Mesh();
        mesh.Vertices.Add(0f, 0f, 0f);
        mesh.Vertices.Add(1f, 0f, 0f);
        mesh.Vertices.Add(0.5f, 1e-7f, 0f);
        mesh.Faces.AddFace(0, 1, 2);
        mesh.Faces.AddFace(0, 1, 1);

        // Act
        var result = DelaunayRefiner.Refine(mesh);

        // Assert
        Assert.That(result.Faces.Count, Is.EqualTo(1));
    }

    private record DelaunayTestData(string CaseName, Mesh Input, int ExpectedTriangleCount, double ExpectedAverageArea);

    private static readonly List<DelaunayTestData> _testData = BuildTestData();

    private static List<DelaunayTestData> BuildTestData()
    {
        return
        [
            new(CaseName: "SimpleSquare", Input: MakeSquareMesh(), ExpectedTriangleCount: 2, ExpectedAverageArea: 50),
            new(CaseName: "LShape", Input: MakeLShapeMesh(), ExpectedTriangleCount: 4, ExpectedAverageArea: 18.75),
            new(CaseName: "Jagged", Input: MakeJaggedMesh(), ExpectedTriangleCount: 9, ExpectedAverageArea: 11.72),
            new(CaseName: "UShape", Input: MakeUShapeMesh(), ExpectedTriangleCount: 6, ExpectedAverageArea: 12),
        ];
    }

    private static Mesh TriangulateProfile(Polygon2D poly)
    {
        var brep = Brep.CreatePlanarBreps(poly.Geometry, 1e-6)[0];
        var profile = new Profile2D(brep, poly, []);
        return profile.Triangulate();
    }

    private static Mesh MakeSquareMesh()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        return TriangulateProfile(poly);
    }

    private static Mesh MakeLShapeMesh()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 5, 0), new Point3d(5, 5, 0),
            new Point3d(5, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        return TriangulateProfile(poly);
    }

    private static Mesh MakeJaggedMesh()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 2, 0), new Point3d(10, 2, 0),
            new Point3d(9, 6, 0), new Point3d(13, 12, 0),
            new Point3d(15, 8, 0), new Point3d(20, 13, 0),
            new Point3d(18, 18, 0), new Point3d(13, 16, 0),
            new Point3d(9, 21, 0), new Point3d(11, 14, 0),
            new Point3d(4, 4, 0), new Point3d(0, 2, 0),
        ]));
        return TriangulateProfile(poly);
    }

    private static Mesh MakeUShapeMesh()
    {
        var poly = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(7, 10, 0),
            new Point3d(7, 3, 0), new Point3d(3, 3, 0),
            new Point3d(3, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        return TriangulateProfile(poly);
    }
}
