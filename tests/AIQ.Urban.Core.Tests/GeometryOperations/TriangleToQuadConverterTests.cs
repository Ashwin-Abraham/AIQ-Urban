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
public class TriangleToQuadConverterTests
{
    [Test]
    [TestCase("TwoTrianglesSquare")]
    [TestCase("SixTrianglesGrid")]
    [TestCase("FanTriangles")]
    [TestCase("LShape")]
    [TestCase("UShape")]
    public void Convert_ProducesValidMixedMesh(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);

        // Arrange
        var input = testData.Input;

        // Act
        var result = input.ToQuadMesh();

        // Visualise
#if DEBUG
        PreviewMesh(testName, result, input);
#endif

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Vertices.Count, Is.EqualTo(input.Vertices.Count));
        Assert.That(result.Faces.Count, Is.EqualTo(testData.ExpectedFaceCount));

        var inputArea = AreaMassProperties.Compute(input).Area;
        var resultArea = AreaMassProperties.Compute(result).Area;
        Assert.That(resultArea, Is.EqualTo(inputArea).Within(1e-4));
    }

    [Test]
    public void Convert_EmptyMesh_ReturnsEmptyMesh()
    {
        // Arrange
        var input = new Mesh();

        // Act
        var result = input.ToQuadMesh();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Vertices.Count, Is.Zero);
        Assert.That(result.Faces.Count, Is.Zero);
    }

    [Test]
    public void Convert_SingleTriangle_ReturnsSameTriangle()
    {
        // Arrange
        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);
        mesh.Vertices.Add(10, 0, 0);
        mesh.Vertices.Add(5, 10, 0);
        mesh.Faces.AddFace(0, 1, 2, 2);

        // Act
        var result = mesh.ToQuadMesh();

        // Assert
        Assert.That(result.Vertices.Count, Is.EqualTo(3));
        Assert.That(result.Faces.Count, Is.EqualTo(1));
        Assert.That(result.Faces[0].IsTriangle, Is.True);
    }

    [Test]
    public void Convert_NonTriangularFaces_ThrowsArgumentException()
    {
        // Arrange
        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);
        mesh.Vertices.Add(10, 0, 0);
        mesh.Vertices.Add(10, 10, 0);
        mesh.Vertices.Add(0, 10, 0);
        mesh.Faces.AddFace(0, 1, 2, 3);

        // Act & Assert
        Assert.That(() => mesh.ToQuadMesh(), Throws.ArgumentException);
    }

    [Test]
    public void Convert_NullMesh_ThrowsArgumentNullException()
    {
        Mesh mesh = null!;

        Assert.That(() => mesh.ToQuadMesh(), Throws.ArgumentNullException);
    }

    private record ConverterTestData(
        string CaseName,
        Mesh Input,
        int ExpectedFaceCount);

    private static readonly List<ConverterTestData> _testData = BuildTestData();

    private static List<ConverterTestData> BuildTestData()
    {
        return
        [
            new(CaseName: "TwoTrianglesSquare",
                Input: MakeTwoTriangleSquare(),
                ExpectedFaceCount: 1),
            new(CaseName: "SixTrianglesGrid",
                Input: MakeSixTriangleGrid(),
                ExpectedFaceCount: 3),
            new(CaseName: "FanTriangles",
                Input: MakeFanTriangles(),
                ExpectedFaceCount: 3),
            new(CaseName: "LShape",
                Input: MakeLShapeMesh(),
                ExpectedFaceCount: 2),
            new(CaseName: "UShape",
                Input: MakeUShapeMesh(),
                ExpectedFaceCount: 3),
        ];
    }

    private static Mesh MakeTwoTriangleSquare()
    {
        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);  // 0
        mesh.Vertices.Add(10, 0, 0); // 1
        mesh.Vertices.Add(10, 10, 0);// 2
        mesh.Vertices.Add(0, 10, 0); // 3
        mesh.Faces.AddFace(0, 1, 2, 2);
        mesh.Faces.AddFace(0, 2, 3, 3);
        return mesh;
    }

    private static Mesh MakeSixTriangleGrid()
    {
        // 2 columns x 3 rows of triangles forming 3 quads
        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);
        mesh.Vertices.Add(10, 0, 0);
        mesh.Vertices.Add(20, 0, 0);
        mesh.Vertices.Add(0, 10, 0);
        mesh.Vertices.Add(10, 10, 0);
        mesh.Vertices.Add(20, 10, 0);
        mesh.Vertices.Add(0, 20, 0);
        mesh.Vertices.Add(10, 20, 0);
        mesh.Vertices.Add(20, 20, 0);
        mesh.Faces.AddFace(0, 1, 4, 4);
        mesh.Faces.AddFace(0, 4, 3, 3);
        mesh.Faces.AddFace(1, 2, 5, 5);
        mesh.Faces.AddFace(1, 5, 4, 4);
        mesh.Faces.AddFace(3, 4, 7, 7);
        mesh.Faces.AddFace(3, 7, 6, 6);
        return mesh;
    }

    private static Mesh MakeFanTriangles()
    {
        var mesh = new Mesh();
        mesh.Vertices.Add(10, 10, 0); // 0 - center
        mesh.Vertices.Add(0, 10, 0);  // 1
        mesh.Vertices.Add(5, 18, 0);  // 2
        mesh.Vertices.Add(15, 18, 0); // 3
        mesh.Vertices.Add(20, 10, 0); // 4
        mesh.Vertices.Add(15, 2, 0);  // 5
        mesh.Vertices.Add(5, 2, 0);   // 6
        mesh.Faces.AddFace(0, 1, 2, 2);
        mesh.Faces.AddFace(0, 2, 3, 3);
        mesh.Faces.AddFace(0, 3, 4, 4);
        mesh.Faces.AddFace(0, 4, 5, 5);
        mesh.Faces.AddFace(0, 5, 6, 6);
        mesh.Faces.AddFace(0, 6, 1, 1);
        return mesh;
    }

    private static Mesh MakeLShapeMesh()
    {
        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);
        mesh.Vertices.Add(10, 0, 0);
        mesh.Vertices.Add(10, 5, 0);
        mesh.Vertices.Add(5, 5, 0);
        mesh.Vertices.Add(5, 10, 0);
        mesh.Vertices.Add(0, 10, 0);
        mesh.Faces.AddFace(0, 1, 3, 3);
        mesh.Faces.AddFace(1, 2, 3, 3);
        mesh.Faces.AddFace(0, 3, 5, 5);
        mesh.Faces.AddFace(3, 4, 5, 5);
        return mesh;
    }

    private static Mesh MakeUShapeMesh()
    {
        var mesh = new Mesh();
        mesh.Vertices.Add(0, 0, 0);
        mesh.Vertices.Add(10, 0, 0);
        mesh.Vertices.Add(10, 10, 0);
        mesh.Vertices.Add(7, 10, 0);
        mesh.Vertices.Add(7, 3, 0);
        mesh.Vertices.Add(3, 3, 0);
        mesh.Vertices.Add(3, 10, 0);
        mesh.Vertices.Add(0, 10, 0);
        mesh.Faces.AddFace(0, 1, 5, 5);
        mesh.Faces.AddFace(1, 4, 5, 5);
        mesh.Faces.AddFace(1, 2, 4, 4);
        mesh.Faces.AddFace(2, 3, 4, 4);
        mesh.Faces.AddFace(0, 5, 7, 7);
        mesh.Faces.AddFace(5, 6, 7, 7);
        return mesh;
    }

#if DEBUG
    private static void PreviewMesh(string name, Mesh result, Mesh input)
    {
        var doc = RhinoDoc.CreateHeadless(null);
        var inputAttribs = RhinoHelper.MakeAttributes(doc, $"{name}::Input", Color.LightGray);
        var inputCopy = input.DuplicateMesh();
        inputCopy.Translate(0, 0, -0.1);
        doc.Objects.AddMesh(inputCopy, inputAttribs);

        var resultAttribs = RhinoHelper.MakeAttributes(doc, $"{name}::Result", Color.CornflowerBlue);
        doc.Objects.AddMesh(result, resultAttribs);

        var quadEdgesAttribs = RhinoHelper.MakeAttributes(doc, $"{name}::Quads", Color.DarkOrange);
        var edgeCount = result.TopologyEdges.Count;
        for (var i = 0; i < edgeCount; i++)
            doc.Objects.AddCurve(result.TopologyEdges.EdgeLine(i).ToNurbsCurve(), quadEdgesAttribs);

        Visualizer.View(doc);
    }
#endif
}
