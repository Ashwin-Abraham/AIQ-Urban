using System;
using System.Linq;
using AIQ.Urban.Core.Extensions;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Extensions;

[RhinoTestFixture]
public class CurveExtensionsSelfIntersectionTests
{
    [TestCase("Crossing", true)]
    [TestCase("TangentialContact", true)]
    [TestCase("Overlap", true)]
    [TestCase("ClosedSeam", false)]
    public void IsSelfIntersecting_CurveShape_ReturnsExpectedResult(string caseName, bool expected)
    {
        // Arrange
        var testData = GetTestData(caseName);
        var curve = new PolylineCurve(testData.Coordinates
            .Select(point => new Point3d(point[0], point[1], 0)));

        // Act
        var result = curve.IsSelfIntersecting();

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    private static SelfIntersectionTestData GetTestData(string caseName)
    {
        return caseName switch
        {
            "Crossing" => new(
                CaseName: "Crossing",
                Coordinates: [[0, 0], [10, 10], [0, 10], [10, 0]]),
            "TangentialContact" => new(
                CaseName: "TangentialContact",
                Coordinates: [[0, 0], [10, 0], [10, 10], [5, 0], [0, 10]]),
            "Overlap" => new(
                CaseName: "Overlap",
                Coordinates: [[0, 0], [10, 0], [10, 10], [5, 10], [5, 0], [15, 0]]),
            "ClosedSeam" => new(
                CaseName: "ClosedSeam",
                Coordinates: [[0, 0], [10, 0], [10, 10], [0, 10], [0, 0]]),
            _ => throw new ArgumentException($"Unknown case: {caseName}", nameof(caseName))
        };
    }

    private record SelfIntersectionTestData(string CaseName, double[][] Coordinates);
}
