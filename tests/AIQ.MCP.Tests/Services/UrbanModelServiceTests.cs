using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.MCP.Models;
using AIQ.MCP.Services;
using NUnit.Framework;
using Rhino.Geometry;
using Rhino.Testing.Fixtures;
using System.Collections.Generic;

namespace AIQ.MCP.Tests.Services;

[RhinoTestFixture]
public class UrbanModelServiceTests
{
    private UrbanModelService _service = null!;

    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
        _service = new UrbanModelService();
    }

    // --- CreateTransitSteps ---

    [Test]
    [TestCase("SingleTransit")]
    [TestCase("MultipleTransits")]
    public void CreateTransitSteps_ValidRequest_ReturnsConfiguration(string testName)
    {
        var testData = _transitTestData.Single(x => x.CaseName == testName);

        // Arrange
        var request = new AddTransitRequest(testData.Input);

        // Act
        var result = _service.CreateTransitSteps(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.TransitData, Has.Count.EqualTo(testData.ExpectedCount));
        Assert.That(result.CenterlineIds, Has.Count.EqualTo(testData.ExpectedCount));

        for (int i = 0; i < testData.ExpectedCount; i++)
        {
            Assert.That(result.TransitData[i].Width, Is.EqualTo(testData.Input[i].Width));
            Assert.That(result.TransitData[i].Centerline, Is.Not.Null);
            Assert.That(result.TransitData[i].Centerline.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.CenterlineIds[i], Is.EqualTo(result.TransitData[i].Centerline.Id));
        }
    }

    [Test]
    public void CreateTransitSteps_NullRequest_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            _service.CreateTransitSteps(null!));
        Assert.That(ex!.Message, Does.Contain("Value cannot be null"));
    }

    [Test]
    public void CreateTransitSteps_NullItems_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            _service.CreateTransitSteps(new AddTransitRequest(null!)));
        Assert.That(ex!.Message, Does.Contain("Value cannot be null"));
    }

    [Test]
    [TestCase("EmptyItems")]
    [TestCase("ZeroWidth")]
    [TestCase("NegativeWidth")]
    public void CreateTransitSteps_InvalidInput_Throws(string testName)
    {
        var testData = _invalidTransitTestData.Single(x => x.CaseName == testName);

        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            _service.CreateTransitSteps(testData.Request!));
        Assert.That(ex!.Message, Does.Contain(testData.ExpectedMessageFragment));
    }

    private record TransitTestData(
        string CaseName,
        List<TransitItem> Input,
        int ExpectedCount);

    private static readonly List<TransitTestData> _transitTestData =
    [
        new("SingleTransit",
        [
            new TransitItem([[0, 0], [10, 0]], 7.0, "Road")
        ], 1),
        new("MultipleTransits",
        [
            new TransitItem([[0, 0], [10, 0]], 7.0, "Road"),
            new TransitItem([[0, 5], [10, 5]], 3.0, "CyclePath"),
        ], 2),
    ];

    private record InvalidTransitTestData(
        string CaseName,
        AddTransitRequest Request,
        string ExpectedMessageFragment);

    private static readonly List<InvalidTransitTestData> _invalidTransitTestData =
    [
        new("EmptyItems", new AddTransitRequest([]), "At least one"),
        new("ZeroWidth", new AddTransitRequest([
            new TransitItem([[0, 0], [10, 0]], 0, "Road")
        ]), "must be positive"),
        new("NegativeWidth", new AddTransitRequest([
            new TransitItem([[0, 0], [10, 0]], -1, "Road")
        ]), "must be positive"),
    ];

    // --- CreateBuildingSteps ---

    [Test]
    [TestCase("SingleBuilding")]
    [TestCase("MultipleBuildings")]
    public void CreateBuildingSteps_ValidRequest_ReturnsConfiguration(string testName)
    {
        var testData = _buildingTestData.Single(x => x.CaseName == testName);

        // Arrange
        var request = new AddBuildingRequest(testData.Input);

        // Act
        var result = _service.CreateBuildingSteps(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.BuildingData, Has.Count.EqualTo(testData.ExpectedCount));
        Assert.That(result.InputCurveIds, Has.Count.EqualTo(testData.ExpectedCount));

        for (int i = 0; i < testData.ExpectedCount; i++)
        {
            Assert.That(result.BuildingData[i].TypicalFloorHeight, Is.EqualTo(testData.Input[i].TypicalFloorHeight));
            Assert.That(result.BuildingData[i].FloorCount, Is.EqualTo(testData.Input[i].FloorCount));
            Assert.That(result.BuildingData[i].Footprint, Is.Not.Null);
            Assert.That(result.BuildingData[i].Footprint.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.InputCurveIds[i], Is.EqualTo(result.BuildingData[i].Footprint.Id));
        }
    }

    [Test]
    public void CreateBuildingSteps_NullRequest_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            _service.CreateBuildingSteps(null!));
        Assert.That(ex!.Message, Does.Contain("Value cannot be null"));
    }

    [Test]
    public void CreateBuildingSteps_NullItems_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            _service.CreateBuildingSteps(new AddBuildingRequest(null!)));
        Assert.That(ex!.Message, Does.Contain("Value cannot be null"));
    }

    [Test]
    [TestCase("EmptyItems")]
    [TestCase("ZeroHeight")]
    [TestCase("ZeroFloorCount")]
    [TestCase("UnknownUseName")]
    public void CreateBuildingSteps_InvalidInput_Throws(string testName)
    {
        var testData = _invalidBuildingTestData.Single(x => x.CaseName == testName);

        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            _service.CreateBuildingSteps(testData.Request!));
        Assert.That(ex!.Message, Does.Contain(testData.ExpectedMessageFragment));
    }

    private record BuildingTestData(
        string CaseName,
        List<BuildingItem> Input,
        int ExpectedCount);

    private static readonly List<BuildingTestData> _buildingTestData =
    [
        new("SingleBuilding",
        [
            new BuildingItem([[0, 0], [10, 0], [10, 10], [0, 10], [0, 0]], 3.5, 5, "Residential")
        ], 1),
        new("MultipleBuildings",
        [
            new BuildingItem([[0, 0], [10, 0], [10, 10], [0, 10], [0, 0]], 3.5, 5, "Residential"),
            new BuildingItem([[0, 0], [5, 0], [5, 5], [0, 5], [0, 0]], 4.0, 3, "Commercial"),
        ], 2),
    ];

    private record InvalidBuildingTestData(
        string CaseName,
        AddBuildingRequest Request,
        string ExpectedMessageFragment);

    private static readonly List<InvalidBuildingTestData> _invalidBuildingTestData =
    [
        new("EmptyItems", new AddBuildingRequest([]), "At least one"),
        new("ZeroHeight", new AddBuildingRequest([
            new BuildingItem([[0, 0], [10, 0], [10, 10], [0, 10], [0, 0]], 0, 5, "Residential")
        ]), "must be positive"),
        new("ZeroFloorCount", new AddBuildingRequest([
            new BuildingItem([[0, 0], [10, 0], [10, 10], [0, 10], [0, 0]], 3.5, 0, "Residential")
        ]), "must be positive"),
        new("UnknownUseName", new AddBuildingRequest([
            new BuildingItem([[0, 0], [10, 0], [10, 10], [0, 10], [0, 0]], 3.5, 5, "InvalidUse")
        ]), "Unknown use name"),
    ];

    // --- CreateChangeUseSteps ---

    [Test]
    public void CreateChangeUseSteps_SingleUse_ReturnsSingleStep()
    {
        // Arrange
        var request = new ChangeBuildingUseRequest([
            new ChangeUseItem(Guid.NewGuid(), "Commercial"),
        ]);

        // Act
        var result = _service.CreateChangeUseSteps(request);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.InstanceOf<ChangeBuildingUseStep>());
    }

    [Test]
    public void CreateChangeUseSteps_MultipleBuildingsSameUse_ReturnsSingleStep()
    {
        // Arrange
        var request = new ChangeBuildingUseRequest([
            new ChangeUseItem(Guid.NewGuid(), "Commercial"),
            new ChangeUseItem(Guid.NewGuid(), "Commercial"),
            new ChangeUseItem(Guid.NewGuid(), "Commercial"),
        ]);

        // Act
        var result = _service.CreateChangeUseSteps(request);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public void CreateChangeUseSteps_MultipleUses_ReturnsMultipleSteps()
    {
        // Arrange
        var request = new ChangeBuildingUseRequest([
            new ChangeUseItem(Guid.NewGuid(), "Commercial"),
            new ChangeUseItem(Guid.NewGuid(), "Residential"),
        ]);

        // Act
        var result = _service.CreateChangeUseSteps(request);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void CreateChangeUseSteps_EmptyGuid_Throws()
    {
        // Arrange
        var request = new ChangeBuildingUseRequest([
            new ChangeUseItem(Guid.Empty, "Commercial"),
        ]);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            _service.CreateChangeUseSteps(request));
        Assert.That(ex!.Message, Does.Contain("cannot be empty"));
    }

    [Test]
    public void CreateChangeUseSteps_NullRequest_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            _service.CreateChangeUseSteps(null!));
        Assert.That(ex!.Message, Does.Contain("Value cannot be null"));
    }

    [Test]
    public void CreateChangeUseSteps_NullItems_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            _service.CreateChangeUseSteps(new ChangeBuildingUseRequest(null!)));
        Assert.That(ex!.Message, Does.Contain("Value cannot be null"));
    }

    [Test]
    [TestCase("EmptyItems")]
    [TestCase("UnknownUseName")]
    public void CreateChangeUseSteps_InvalidInput_Throws(string testName)
    {
        var testData = _invalidChangeUseTestData.Single(x => x.CaseName == testName);

        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            _service.CreateChangeUseSteps(testData.Request!));
        Assert.That(ex!.Message, Does.Contain(testData.ExpectedMessageFragment));
    }

    private record InvalidChangeUseTestData(
        string CaseName,
        ChangeBuildingUseRequest Request,
        string ExpectedMessageFragment);

    private static readonly List<InvalidChangeUseTestData> _invalidChangeUseTestData =
    [
        new("EmptyItems", new ChangeBuildingUseRequest([]), "At least one"),
        new("UnknownUseName", new ChangeBuildingUseRequest([
            new ChangeUseItem(Guid.NewGuid(), "InvalidUse")
        ]), "Unknown use name"),
    ];

    // --- GetElementSummaries ---

    [Test]
    public void GetElementSummaries_EmptyModel_ReturnsEmptyLists()
    {
        // Arrange (model already reset in SetUp)

        // Act
        var result = _service.GetElementSummaries();

        // Assert
        Assert.That(result.Transits, Is.Empty);
        Assert.That(result.Buildings, Is.Empty);
        Assert.That(result.Parcels, Is.Empty);
        Assert.That(result.Zones, Is.Empty);
    }

    [Test]
    public void GetElementSummaries_PopulatedModel_ReturnsAllElementTypes()
    {
        // Arrange - populate the model
        var centerline = new Curve2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0)
        ]), Guid.NewGuid());
        UrbanModel.Instance.AddTransit([
            new Transit(centerline, 7.0, TransitType.Road)
        ]);

        var footprint = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(10, 0, 0),
            new Point3d(10, 10, 0), new Point3d(0, 10, 0),
            new Point3d(0, 0, 0),
        ]));
        UrbanModel.Instance.AddBuilding(new Building(footprint, 3.5, 5, DefaultUses.Residential));

        // Manually add a parcel (normally done by RecalculateParcelsStep)
        var parcelOutline = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(20, 0, 0),
            new Point3d(20, 20, 0), new Point3d(0, 20, 0),
            new Point3d(0, 0, 0),
        ]));
        UrbanModel.Instance.Parcels.Add(new Parcel(parcelOutline, DefaultUses.Public));

        var zoneBoundary = new Polygon2D(new PolylineCurve([
            new Point3d(0, 0, 0), new Point3d(30, 0, 0),
            new Point3d(30, 30, 0), new Point3d(0, 30, 0),
            new Point3d(0, 0, 0),
        ]));
        UrbanModel.Instance.Zones.Add(new Zone(zoneBoundary, "TestZone"));

        // Act
        var result = _service.GetElementSummaries();

        // Assert
        Assert.That(result.Transits, Has.Count.EqualTo(1));
        Assert.That(result.Buildings, Has.Count.EqualTo(1));
        Assert.That(result.Parcels, Has.Count.EqualTo(1));
        Assert.That(result.Zones, Has.Count.EqualTo(1));

        // Check transit summary
        var transit = result.Transits[0];
        Assert.That(transit.Type, Is.EqualTo("Transit"));
        Assert.That(transit.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(transit.Summary, Does.Contain("Road"));

        // Check building summary
        var building = result.Buildings[0];
        Assert.That(building.Type, Is.EqualTo("Building"));
        Assert.That(building.Use, Is.EqualTo("Residential"));

        // Check parcel summary
        var parcel = result.Parcels[0];
        Assert.That(parcel.Type, Is.EqualTo("Parcel"));
        Assert.That(parcel.Use, Is.EqualTo("Public"));

        // Check zone summary
        var zone = result.Zones[0];
        Assert.That(zone.Type, Is.EqualTo("Zone"));
        Assert.That(zone.Summary, Does.Contain("TestZone"));
        Assert.That(zone.Details, Is.Not.Null);
        Assert.That(zone.Details!["name"], Is.EqualTo("TestZone"));
    }
}
