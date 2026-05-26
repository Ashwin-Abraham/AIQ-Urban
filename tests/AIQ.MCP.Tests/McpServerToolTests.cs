using AIQ.MCP.Services;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;

using NUnit.Framework;
using Rhino;
using Rhino.Testing.Fixtures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIQ.MCP.Tests;

[RhinoTestFixture]
public class McpServerToolTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    [TestCase("SingleRoad")]
    [TestCase("SingleCyclePath")]
    [TestCase("EmptyCoordinates")]
    public void AddTransit_AddsTransitsToModel(string testName)
    {
        // Arrange
        var testData = _transitTestData.Single(x => x.CaseName == testName);

        using var doc = RhinoDoc.CreateHeadless(null);
        var tools = new UrbanModelTools(doc, UrbanModel.Instance);

        // Act
        var result = tools.AddTransit(testData.Width, testData.Type, testData.CurveCoordinates);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(testData.ExpectedTransitCount));

        if (testData.ExpectedRhinoLayerCounts is { Count: > 0 })
        {
            foreach (var (layer, count) in testData.ExpectedRhinoLayerCounts)
                AssertLayerObjectCount(doc, layer, count);
        }
    }

    [Test]
    [TestCase("SingleBuilding")]
    [TestCase("MultipleBuildings")]
    [TestCase("EmptyFootprints")]
    public void AddBuilding_AddsBuildingsToModel(string testName)
    {
        // Arrange
        var testData = _buildingTestData.Single(x => x.CaseName == testName);

        using var doc = RhinoDoc.CreateHeadless(null);
        var tools = new UrbanModelTools(doc, UrbanModel.Instance);

        // Act
        var result = tools.AddBuilding(testData.FloorHeight, testData.FloorCount, testData.Use, testData.FootprintCurves);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(UrbanModel.Instance.Buildings, Has.Count.EqualTo(testData.ExpectedBuildingCount));

        if (testData.ExpectedRhinoLayerCounts is { Count: > 0 })
        {
            foreach (var (layer, count) in testData.ExpectedRhinoLayerCounts)
                AssertLayerObjectCount(doc, layer, count);
        }
    }

    private static void AssertLayerObjectCount(RhinoDoc doc, string layerPath, int expectedCount)
    {
        var layerIndex = doc.Layers.FindByFullPath(layerPath, -1);
        if (layerIndex < 0)
        {
            Assert.That(expectedCount, Is.EqualTo(0), $"Layer '{layerPath}' does not exist but expected {expectedCount} objects");
            return;
        }

        var actualCount = 0;
        foreach (var obj in doc.Objects)
        {
            if (obj != null && obj.Attributes.LayerIndex == layerIndex)
                actualCount++;
        }

        Assert.That(actualCount, Is.EqualTo(expectedCount), $"Expected {expectedCount} objects on layer '{layerPath}' but found {actualCount}");
    }

    private record AddTransitTestData(
        string CaseName,
        double Width,
        TransitType Type,
        List<CurveCoordinates> CurveCoordinates,
        int ExpectedTransitCount,
        Dictionary<string, int>? ExpectedRhinoLayerCounts = null);

    private record AddBuildingTestData(
        string CaseName,
        double FloorHeight,
        int FloorCount,
        Use Use,
        List<CurveCoordinates> FootprintCurves,
        int ExpectedBuildingCount,
        Dictionary<string, int>? ExpectedRhinoLayerCounts = null);

    private static readonly List<AddTransitTestData> _transitTestData =
    [
        new AddTransitTestData(
            CaseName: "SingleRoad",
            Width: 6.0,
            Type: TransitType.Road,
            CurveCoordinates:
            [
                new CurveCoordinates([(0.0, 0.0), (50.0, 0.0)]),
            ],
            ExpectedTransitCount: 1,
            ExpectedRhinoLayerCounts: new Dictionary<string, int>
            {
                ["AIQ Urban::Transit::Road::Outline::6"] = 1,
                ["AIQ Urban::Transit::Road::Centerline::6"] = 1,
                ["AIQ Urban::Transit::Road::Surface"] = 1,
            }),

        new AddTransitTestData(
            CaseName: "SingleCyclePath",
            Width: 3.0,
            Type: TransitType.CyclePath,
            CurveCoordinates:
            [
                new CurveCoordinates([(0.0, 0.0), (30.0, 10.0)]),
            ],
            ExpectedTransitCount: 1,
            ExpectedRhinoLayerCounts: new Dictionary<string, int>
            {
                ["AIQ Urban::Transit::CyclePath::Outline::3"] = 1,
                ["AIQ Urban::Transit::CyclePath::Centerline::3"] = 1,
                ["AIQ Urban::Transit::CyclePath::Surface"] = 1,
            }),

        new AddTransitTestData(
            CaseName: "EmptyCoordinates",
            Width: 6.0,
            Type: TransitType.Road,
            CurveCoordinates: [],
            ExpectedTransitCount: 0),
    ];

    private static readonly List<AddBuildingTestData> _buildingTestData =
    [
        new AddBuildingTestData(
            CaseName: "SingleBuilding",
            FloorHeight: 3.5,
            FloorCount: 5,
            Use: DefaultUses.Residential,
            FootprintCurves:
            [
                new CurveCoordinates([(0.0, 0.0), (10.0, 0.0), (10.0, 10.0), (0.0, 10.0), (0.0, 0.0)]),
            ],
            ExpectedBuildingCount: 1,
            ExpectedRhinoLayerCounts: new Dictionary<string, int>
            {
                ["AIQ Urban::Building::Envelope"] = 1,
                ["AIQ Urban::Building::FloorPlates::Residential"] = 5,
            }),

        new AddBuildingTestData(
            CaseName: "MultipleBuildings",
            FloorHeight: 3.5,
            FloorCount: 5,
            Use: DefaultUses.Residential,
            FootprintCurves:
            [
                new CurveCoordinates([(0.0, 0.0), (10.0, 0.0), (10.0, 10.0), (0.0, 10.0), (0.0, 0.0)]),
                new CurveCoordinates([(20.0, 20.0), (30.0, 20.0), (30.0, 30.0), (20.0, 30.0), (20.0, 20.0)]),
            ],
            ExpectedBuildingCount: 2,
            ExpectedRhinoLayerCounts: new Dictionary<string, int>
            {
                ["AIQ Urban::Building::Envelope"] = 2,
                ["AIQ Urban::Building::FloorPlates::Residential"] = 10,
            }),

        new AddBuildingTestData(
            CaseName: "EmptyFootprints",
            FloorHeight: 3.5,
            FloorCount: 5,
            Use: DefaultUses.Residential,
            FootprintCurves: [],
            ExpectedBuildingCount: 0),
    ];
}
