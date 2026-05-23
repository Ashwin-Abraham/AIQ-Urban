using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;
using AIQ.MCP.Services;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.MCP.Tests.Services;

[RhinoTestFixture]
public class GeometryFactoryTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    // --- CreateCenterline ---

    [Test]
    [TestCase("TwoPoints")]
    [TestCase("ThreePoints")]
    [TestCase("FivePoints")]
    public void CreateCenterline_ReturnsValidCurve2D(string testName)
    {
        var testData = _centerlineTestData.Single(x => x.CaseName == testName);

        // Arrange
        // Act
        var result = GeometryFactory.CreateCenterline(testData.Input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
        var polyline = result.Geometry as PolylineCurve;
        Assert.That(polyline, Is.Not.Null);
        Assert.That(polyline!.PointCount, Is.EqualTo(testData.ExpectedPointCount));
        Assert.That(result.Geometry.IsClosed, Is.False);
        Assert.That(result.Geometry.IsPlanar(), Is.True);
    }

    [Test]
    public void CreateCenterline_NullInput_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            GeometryFactory.CreateCenterline(null!));
        Assert.That(ex!.Message, Does.Contain("Value cannot be null"));
    }

    [Test]
    [TestCase("SinglePoint")]
    [TestCase("EmptyArray")]
    public void CreateCenterline_InvalidInput_Throws(string testName)
    {
        var testData = _invalidCenterlineTestData.Single(x => x.CaseName == testName);

        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            GeometryFactory.CreateCenterline(testData.Input));
        Assert.That(ex!.Message, Does.Contain(testData.ExpectedMessageFragment));
    }

    private record CenterlineTestData(
        string CaseName,
        double[][] Input,
        int ExpectedPointCount);

    private static readonly List<CenterlineTestData> _centerlineTestData =
    [
        new("TwoPoints", [[0, 0], [10, 0]], 2),
        new("ThreePoints", [[0, 0], [10, 0], [10, 10]], 3),
        new("FivePoints", [[0, 0], [5, 0], [5, 5], [10, 5], [10, 10]], 5),
    ];

    private record InvalidInputTestData(
        string CaseName,
        double[][] Input,
        string ExpectedMessageFragment);

    private static readonly List<InvalidInputTestData> _invalidCenterlineTestData =
    [
        new("SinglePoint", [[0, 0]], "at least 2"),
        new("EmptyArray", [], "at least 2"),
    ];

    // --- CreateFootprint ---

    [Test]
    [TestCase("SquareClosed")]
    [TestCase("TriangleAutoClosed")]
    [TestCase("PentagonExplicitClosed")]
    public void CreateFootprint_ReturnsValidPolygon2D(string testName)
    {
        var testData = _footprintTestData.Single(x => x.CaseName == testName);

        // Arrange
        // Act
        var result = GeometryFactory.CreateFootprint(testData.Input);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.Geometry.IsClosed, Is.True);
        Assert.That(result.Geometry.IsPlanar(), Is.True);
        var polyline = result.Geometry as PolylineCurve;
        Assert.That(polyline, Is.Not.Null);
        Assert.That(polyline!.PointCount, Is.EqualTo(testData.ExpectedPointCount));
    }

    [Test]
    public void CreateFootprint_NullInput_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            GeometryFactory.CreateFootprint(null!));
        Assert.That(ex!.Message, Does.Contain("Value cannot be null"));
    }

    [Test]
    [TestCase("TwoPoints")]
    [TestCase("EmptyArray")]
    public void CreateFootprint_InvalidInput_Throws(string testName)
    {
        var testData = _invalidFootprintTestData.Single(x => x.CaseName == testName);

        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            GeometryFactory.CreateFootprint(testData.Input));
        Assert.That(ex!.Message, Does.Contain(testData.ExpectedMessageFragment));
    }

    private record FootprintTestData(
        string CaseName,
        double[][] Input,
        int ExpectedPointCount);

    private static readonly List<FootprintTestData> _footprintTestData =
    [
        // Explicitly closed (first = last)
        new("SquareClosed", [[0, 0], [10, 0], [10, 10], [0, 10], [0, 0]], 5),
        // Auto-closed (last != first)
        new("TriangleAutoClosed", [[0, 0], [10, 0], [5, 10]], 4),
        // Explicitly closed pentagon
        new("PentagonExplicitClosed", [[0, 0], [5, 0], [6, 5], [3, 8], [-2, 4], [0, 0]], 6),
    ];

    private static readonly List<InvalidInputTestData> _invalidFootprintTestData =
    [
        new("TwoPoints", [[0, 0], [10, 0]], "at least 3"),
        new("EmptyArray", [], "at least 3"),
    ];

    // --- ResolveTransitType ---

    [Test]
    [TestCase("Road", TransitType.Road)]
    [TestCase("road", TransitType.Road)]
    [TestCase("ROAD", TransitType.Road)]
    [TestCase("Railway", TransitType.Railway)]
    [TestCase("CyclePath", TransitType.CyclePath)]
    [TestCase("Pedestrian", TransitType.Pedestrian)]
    [TestCase("Waterway", TransitType.Waterway)]
    public void ResolveTransitType_ValidName_ReturnsEnum(string name, TransitType expected)
    {
        // Act
        var result = GeometryFactory.ResolveTransitType(name);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    [TestCase("InvalidType")]
    [TestCase("Roadd")]
    public void ResolveTransitType_InvalidName_Throws(string name)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            GeometryFactory.ResolveTransitType(name));
        Assert.That(ex!.Message, Does.Contain("Unknown transit type"));
        Assert.That(ex.Message, Does.Contain("Road"));
    }

    // --- ResolveUse ---

    [Test]
    [TestCase("Residential")]
    [TestCase("residential")]
    [TestCase("RESIDENTIAL")]
    [TestCase("Commercial")]
    [TestCase("Retail")]
    [TestCase("Industrial")]
    [TestCase("MixedUse")]
    [TestCase("Public")]
    [TestCase("Park")]
    [TestCase("Parking")]
    [TestCase("Utility")]
    public void ResolveUse_ValidName_ReturnsUse(string name)
    {
        // Act
        var result = GeometryFactory.ResolveUse(name);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo(name).IgnoreCase);
    }

    [Test]
    [TestCase("InvalidUse")]
    [TestCase("Residential ")]
    public void ResolveUse_InvalidName_Throws(string name)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            GeometryFactory.ResolveUse(name));
        Assert.That(ex!.Message, Does.Contain("Unknown use name"));
        Assert.That(ex.Message, Does.Contain("Residential"));
    }
}
