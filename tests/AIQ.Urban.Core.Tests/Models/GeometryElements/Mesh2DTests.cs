using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.PreviewHelper.Core;
using AIQ.Urban.Core.Models.GeometryElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Models.GeometryElements;

[RhinoTestFixture]
public class Mesh2DTests
{
    [Test]
    [TestCase("SimpleSquare")]
    [TestCase("RectangleWithHole")]
    [TestCase("LShape")]
    [TestCase("TwoHoles")]
    public void Create_ProducesValid2DMesh(string testName)
    {
        var testData = _testData.Single(x => x.CaseName == testName);
        var profile = testData.Input;

        var result = Mesh2D.Create(profile);

#if DEBUG
        // Preview
        PreviewHub.ResetDoc();
        profile.Outline.Geometry.Translate(0, 0, -1);
        profile.Holes.ForEach(x => x.Geometry.Translate(0, 0, -1));
        profile.AddToPreviewDoc(Color.Beige);
        result.PreviewViaHub(color: Color.Black);
#endif

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Geometry.IsValid, Is.True);
        Assert.That(result.Geometry.Vertices.Count, Is.GreaterThan(0));
        Assert.That(result.Geometry.Faces.Count, Is.GreaterThan(0));
        Assert.That(result.Geometry.Faces.All(f => f.IsTriangle || f.IsQuad), Is.True);

        var meshArea = AreaMassProperties.Compute(result.Geometry).Area;
        var profileArea = AreaMassProperties.Compute(profile.Geometry).Area;
        Assert.That(meshArea, Is.EqualTo(profileArea).Within(1e-4));
    }

    private record Mesh2DTestData(string CaseName, Profile2D Input);

    private static readonly List<Mesh2DTestData> _testData = BuildTestData();

    private static List<Mesh2DTestData> BuildTestData()
    {
        return
        [
            new(CaseName: "SimpleSquare", Input: MakeSquare()),
            new(CaseName: "RectangleWithHole", Input: MakeRectangleWithHole()),
            new(CaseName: "LShape", Input: MakeLShape()),
            new(CaseName: "TwoHoles", Input: MakeTwoHoles()),
        ];
    }

    private static Profile2D MakeSquare()
    {
        return new Profile2D(
            Polygon2D.Create([(0, 0), (10, 0), (10, 10), (0, 10)]),
            []);
    }

    private static Profile2D MakeRectangleWithHole()
    {
        return new Profile2D(
            Polygon2D.Create([(0, 0), (20, 0), (20, 10), (0, 10)]),
            [Polygon2D.Create([(5, 2), (15, 2), (15, 8), (5, 8)])]);
    }

    private static Profile2D MakeLShape()
    {
        return new Profile2D(
            Polygon2D.Create([(0, 0), (10, 0), (10, 5), (5, 5), (5, 10), (0, 10)]),
            []);
    }

    private static Profile2D MakeTwoHoles()
    {
        return new Profile2D(
            Polygon2D.Create([(0, 0), (4,0), (7,2), (10,0), (14, 0), (14, 6), (10, 6), (7, 4), (4, 6), (0, 6)]),
            [
                Polygon2D.Create([(2, 1), (4, 1), (4, 3), (2, 3)]),
                Polygon2D.Create([(10, 1), (12, 1), (12, 3), (10, 3)]),
            ]);
    }
}
