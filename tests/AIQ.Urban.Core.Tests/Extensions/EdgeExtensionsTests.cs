using System.Linq;
using AIQ.Urban.Core.Extensions;
using AIQ.Urban.Core.Models.UrbanGraph;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.Core.Tests.Extensions;

[RhinoTestFixture]
public class EdgeExtensionsTests
{
    [Test]
    [TestCase("Horizontal")]
    [TestCase("Vertical")]
    [TestCase("Diagonal")]
    public void ToLine_ReturnsCorrectLine(string testName)
    {
        // Arrange
        var testData = _testData.Single(x => x.CaseName == testName);

        // Act
        var result = testData.Edge.ToLine();

        // Assert
        Assert.That(result.FromX, Is.EqualTo(testData.Expected.FromX).Within(1e-6));
        Assert.That(result.FromY, Is.EqualTo(testData.Expected.FromY).Within(1e-6));
        Assert.That(result.FromZ, Is.EqualTo(testData.Expected.FromZ).Within(1e-6));
        Assert.That(result.ToX, Is.EqualTo(testData.Expected.ToX).Within(1e-6));
        Assert.That(result.ToY, Is.EqualTo(testData.Expected.ToY).Within(1e-6));
        Assert.That(result.ToZ, Is.EqualTo(testData.Expected.ToZ).Within(1e-6));
    }

    private record EdgeTestData(string CaseName, Edge Edge, Line Expected);

    private static readonly EdgeTestData[] _testData =
    [
        new("Horizontal",
            new Edge(new GraphNode(new Point3d(0, 0, 0), []), new GraphNode(new Point3d(10, 0, 0), [])),
            new Line(new Point3d(0, 0, 0), new Point3d(10, 0, 0))),
        new("Vertical",
            new Edge(new GraphNode(new Point3d(5, 2, 0), []), new GraphNode(new Point3d(5, 8, 0), [])),
            new Line(new Point3d(5, 2, 0), new Point3d(5, 8, 0))),
        new("Diagonal",
            new Edge(new GraphNode(new Point3d(1, 2, 3), []), new GraphNode(new Point3d(4, 5, 6), [])),
            new Line(new Point3d(1, 2, 3), new Point3d(4, 5, 6))),
    ];
}
