using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.Models.GeometryElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Extensions;

[RhinoTestFixture]
public class Polygon2DTests
{
    [Test]
    [TestCase("SquareClosed")]
    [TestCase("TriangleAutoClosed")]
    [TestCase("PentagonExplicitClosed")]
    public void Create_ValidCoords_ReturnsPolygon2D(string testName)
    {
        var testData = _validTestData.Single(x => x.CaseName == testName);

        var result = Polygon2D.Create(testData.Input);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.Geometry.IsClosed, Is.True);
        Assert.That(result.Geometry.IsPlanar(), Is.True);
        var polyline = result.Geometry as PolylineCurve;
        Assert.That(polyline, Is.Not.Null);
        Assert.That(polyline!.PointCount, Is.EqualTo(testData.ExpectedPointCount));
    }

    [Test]
    public void Create_NullInput_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            Polygon2D.Create(null!));
        Assert.That(ex!.Message, Does.Contain("Value cannot be null"));
    }

    [Test]
    [TestCase("TwoPoints")]
    [TestCase("EmptyArray")]
    public void Create_InvalidInput_Throws(string testName)
    {
        var testData = _invalidTestData.Single(x => x.CaseName == testName);

        var ex = Assert.Throws<ArgumentException>(() =>
            Polygon2D.Create(testData.Input));
        Assert.That(ex!.Message, Does.Contain(testData.ExpectedMessageFragment));
    }

    private record TestData(string CaseName, double[][] Input, int ExpectedPointCount);

    private static readonly List<TestData> _validTestData =
    [
        new("SquareClosed", [[0, 0], [10, 0], [10, 10], [0, 10], [0, 0]], 5),
        new("TriangleAutoClosed", [[0, 0], [10, 0], [5, 10]], 4),
        new("PentagonExplicitClosed", [[0, 0], [5, 0], [6, 5], [3, 8], [-2, 4], [0, 0]], 6),
    ];

    private record InvalidTestData(string CaseName, double[][] Input, string ExpectedMessageFragment);

    private static readonly List<InvalidTestData> _invalidTestData =
    [
        new("TwoPoints", [[0, 0], [10, 0]], "required"),
        new("EmptyArray", [], "required"),
    ];
}
