using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.GeometryOperations;
using AIQ.Urban.DebugViz;
using NUnit.Framework;
using Rhino;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.GeometryOperations;

[RhinoTestFixture]
public class DelaunatorTests
{
    [Test]
    [TestCase("ThreePoints")]
    [TestCase("Square")]
    [TestCase("Pentagon")]
    [TestCase("NonDelaunayQuad")]
    [TestCase("Octagon")]
    public void Compute_ReturnsValidDelaunayMesh(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        var sut = new Delaunator(testData.Points);

        // Act
        var result = sut.Run();

        // Visualise
#if DEBUG
        PreviewMesh(testName, result);
#endif

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Faces.Count, Is.EqualTo(testData.ExpectedTriangleCount));
        Assert.That(result.Faces.All(f => f.IsTriangle), Is.True);
        Assert.That(IsDelaunayTriangulation(result), Is.True);
    }

    private record DelaunatorTestData(
        string CaseName,
        Point2d[] Points,
        int ExpectedTriangleCount);

    private static readonly List<DelaunatorTestData> _testData = BuildTestData();

    private static List<DelaunatorTestData> BuildTestData()
    {
        return
        [
            new(CaseName: "ThreePoints", Points: MakeThreePoints(), ExpectedTriangleCount: 1),
            new(CaseName: "Square", Points: MakeSquarePoints(), ExpectedTriangleCount: 2),
            new(CaseName: "Pentagon", Points: MakePentagonPoints(), ExpectedTriangleCount: 3),
            new(CaseName: "NonDelaunayQuad", Points: MakeNonDelaunayQuadPoints(), ExpectedTriangleCount: 2),
            new(CaseName: "Octagon", Points: MakeOctagonPoints(), ExpectedTriangleCount: 6),
        ];
    }

    private static Point2d[] MakeThreePoints()
    {
        return [new(0, 0), new(10, 0), new(5, 10)];
    }

    private static Point2d[] MakeSquarePoints()
    {
        return [new(0, 0), new(10, 0), new(10, 10), new(0, 10)];
    }

    private static Point2d[] MakePentagonPoints()
    {
        return [
            new(0, 0),
            new(10, 0),
            new(12, 6),
            new(5, 12),
            new(-2, 6),
        ];
    }

    private static Point2d[] MakeNonDelaunayQuadPoints()
    {
        return [new(0, 0), new(10, 0), new(9, 10), new(1, 9)];
    }

    private static Point2d[] MakeOctagonPoints()
    {
        return [
            new(1, 0),
            new(2, 0),
            new(2.3, 1),
            new(2.3, 2),
            new(2, 3),
            new(1, 3),
            new(0.9, 2),
            new(0.9, 1),
        ];
    }

    private static bool IsDelaunayTriangulation(Mesh mesh)
    {
        var verts = mesh.Vertices;
        for (var i = 0; i < mesh.Faces.Count; i++)
        {
            var f = mesh.Faces[i];
            if (!f.IsTriangle) return false;
            for (var j = 0; j < verts.Count; j++)
            {
                if (j == f.A || j == f.B || j == f.C) continue;
                if (((Point3d)verts[j]).IsInsideCircumcircle(
                        (Point3d)verts[f.A], (Point3d)verts[f.B], (Point3d)verts[f.C], 1e-5))
                    return false;
            }
        }
        return true;
    }

#if DEBUG
    private static void PreviewMesh(string name, Mesh mesh)
    {
        var doc = RhinoDoc.CreateHeadless(null);
        var attribs = RhinoHelper.MakeAttributes(doc, $"Delaunator::{name}", Color.CornflowerBlue);
        var edgeCount = mesh.TopologyEdges.Count;
        for (var i = 0; i < edgeCount; i++)
            doc.Objects.AddCurve(mesh.TopologyEdges.EdgeLine(i).ToNurbsCurve(), attribs);
        Visualizer.View(doc);
    }
#endif
}
