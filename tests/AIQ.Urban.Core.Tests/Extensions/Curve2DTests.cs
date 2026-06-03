using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core.Models.GeometryElements;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Extensions;

[RhinoTestFixture]
public class Curve2DTests
{
    [Test]
    [TestCase("TwoPoints")]
    [TestCase("ThreePoints")]
    [TestCase("FivePoints")]
    public void Create_ValidCoords_ReturnsCurve2D(string testName)
    {
        var testData = _validTestData.Single(x => x.CaseName == testName);

        var result = Curve2D.Create(testData.Input);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
        var polyline = result.Geometry as PolylineCurve;
        Assert.That(polyline, Is.Not.Null);
        Assert.That(polyline!.PointCount, Is.EqualTo(testData.ExpectedPointCount));
        Assert.That(result.Geometry.IsClosed, Is.False);
        Assert.That(result.Geometry.IsPlanar(), Is.True);
    }

    [Test]
    [TestCase("SinglePoint")]
    [TestCase("EmptyArray")]
    public void Create_InvalidInput_Throws(string testName)
    {
        var testData = _invalidTestData.Single(x => x.CaseName == testName);

        var ex = Assert.Throws<ArgumentException>(() =>
            Curve2D.Create(testData.Input));
        Assert.That(ex!.Message, Does.Contain(testData.ExpectedMessageFragment));
    }

    private record TestData(string CaseName, List<(double X, double Y)> Input, int ExpectedPointCount);

    private static readonly List<TestData> _validTestData =
    [
        new("TwoPoints", [(0, 0), (10, 0)], 2),
        new("ThreePoints", [(0, 0), (10, 0), (10, 10)], 3),
        new("FivePoints", [(0, 0), (5, 0), (5, 5), (10, 5), (10, 10)], 5),
    ];

    private record InvalidTestData(string CaseName, List<(double X, double Y)> Input, string ExpectedMessageFragment);

    private static readonly List<InvalidTestData> _invalidTestData =
    [
        new("SinglePoint", [(0, 0)], "required"),
        new("EmptyArray", [], "required"),
    ];
}
