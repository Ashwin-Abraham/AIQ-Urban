using System.Collections.Generic;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.RhinoPlugin.MCP_Bridge;
using NUnit.Framework;
using Rhino;
using Rhino.Testing.Fixtures;

namespace AIQ.Urban.RhinoInterface.Tests.MCP_Bridge;

[RhinoTestFixture]
public class UrbanModelToolsTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    public void AddTransit_OneSelfIntersectingCenterline_RejectsAtomicBatchAndIdentifiesItem()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var tools = new UrbanModelTools(doc, UrbanModel.Instance);
        List<CurveCoordinates> centerlines =
        [
            new([new(0, 0), new(10, 0)]),
            new([new(0, 0), new(10, 10), new(0, 10), new(10, 0)]),
        ];

        // Act
        var result = tools.AddTransit(6, TransitType.Road, centerlines);

        // Assert
        Assert.That(result, Does.Contain("centerlineCurves[1]"));
        Assert.That(result, Does.Contain("must not self-intersect"));
        Assert.That(UrbanModel.Instance.Transits, Is.Empty);
    }

    [Test]
    public void AddBuilding_OneSelfIntersectingFootprint_RejectsAtomicBatchAndIdentifiesItem()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        var tools = new UrbanModelTools(doc, UrbanModel.Instance);
        List<CurveCoordinates> footprints =
        [
            new([new(0, 0), new(10, 0), new(10, 10), new(0, 10)]),
            new([new(20, 0), new(30, 10), new(20, 10), new(30, 0)]),
        ];

        // Act
        var result = tools.AddBuilding(3, 2, "Residential", footprints);

        // Assert
        Assert.That(result, Does.Contain("footprintCurves[1]"));
        Assert.That(result, Does.Contain("must not self-intersect"));
        Assert.That(UrbanModel.Instance.Buildings, Is.Empty);
    }
}
