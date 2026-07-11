using AIQ.Urban.Core.Enums;
using AIQ.Urban.RhinoInterface.Utils;
using NUnit.Framework;

namespace AIQ.Urban.RhinoInterface.Tests;

public class LayerNameParserTests
{
    [Test]
    public void ParseTransitLayer_RoadCenterline_ReturnsRoadTypeAndWidth()
    {
        // Arrange
        var path = "AIQ Urban::Transit::Road::Centerline::6";

        // Act
        var result = LayerNameParser.ParseTransitLayer(path);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Type, Is.EqualTo(TransitType.Road));
        Assert.That(result.Value.Width, Is.EqualTo(6.0));
    }

    [Test]
    public void ParseTransitLayer_RailwayOutline_ReturnsRailwayTypeAndWidth()
    {
        // Arrange
        var path = "AIQ Urban::Transit::Railway::Outline::3.5";

        // Act
        var result = LayerNameParser.ParseTransitLayer(path);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Type, Is.EqualTo(TransitType.Railway));
        Assert.That(result.Value.Width, Is.EqualTo(3.5).Within(1e-6));
    }

    [Test]
    public void ParseTransitLayer_CaseInsensitiveType_ReturnsParsedType()
    {
        // Arrange
        var path = "AIQ Urban::Transit::road::Centerline::6";

        // Act
        var result = LayerNameParser.ParseTransitLayer(path);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Type, Is.EqualTo(TransitType.Road));
    }

    [Test]
    public void ParseTransitLayer_CyclePathSurface_ReturnsNull()
    {
        // Arrange — Surface format has only 4 parts, not 5
        var path = "AIQ Urban::Transit::CyclePath::Surface";

        // Act
        var result = LayerNameParser.ParseTransitLayer(path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseTransitLayer_PedestrianCenterline_ReturnsPedestrianType()
    {
        // Arrange
        var path = "AIQ Urban::Transit::Pedestrian::Centerline::2";

        // Act
        var result = LayerNameParser.ParseTransitLayer(path);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Type, Is.EqualTo(TransitType.Pedestrian));
        Assert.That(result.Value.Width, Is.EqualTo(2.0));
    }

    [Test]
    public void ParseTransitLayer_WaterwayOutline_ReturnsWaterwayType()
    {
        // Arrange
        var path = "AIQ Urban::Transit::Waterway::Outline::10";

        // Act
        var result = LayerNameParser.ParseTransitLayer(path);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value.Type, Is.EqualTo(TransitType.Waterway));
        Assert.That(result.Value.Width, Is.EqualTo(10.0));
    }

    [Test]
    public void ParseTransitLayer_TooFewParts_ReturnsNull()
    {
        // Arrange
        var path = "AIQ Urban::Transit::Road";

        // Act
        var result = LayerNameParser.ParseTransitLayer(path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseTransitLayer_TooManyParts_ReturnsNull()
    {
        // Arrange
        var path = "AIQ Urban::Transit::Road::Centerline::6::Extra";

        // Act
        var result = LayerNameParser.ParseTransitLayer(path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseTransitLayer_WrongPrefix_ReturnsNull()
    {
        // Arrange
        var path = "Other::Transit::Road::Centerline::6";

        // Act
        var result = LayerNameParser.ParseTransitLayer(path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseTransitLayer_InvalidTransitType_ReturnsNull()
    {
        // Arrange
        var path = "AIQ Urban::Transit::Submarine::Centerline::6";

        // Act
        var result = LayerNameParser.ParseTransitLayer(path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseTransitLayer_InvalidWidth_ReturnsNull()
    {
        // Arrange
        var path = "AIQ Urban::Transit::Road::Centerline::abc";

        // Act
        var result = LayerNameParser.ParseTransitLayer(path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseTransitLayer_InvalidLayerType_ReturnsNull()
    {
        // Arrange — neither Centerline nor Outline
        var path = "AIQ Urban::Transit::Road::Surface::6";

        // Act
        var result = LayerNameParser.ParseTransitLayer(path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseFloorPlateUse_Residential_ReturnsResidential()
    {
        // Arrange
        var path = "AIQ Urban::Building::FloorPlates::Residential";

        // Act
        var result = LayerNameParser.ParseFloorPlateUse(path);

        // Assert
        Assert.That(result, Is.EqualTo("Residential"));
    }

    [Test]
    public void ParseFloorPlateUse_Commercial_ReturnsCommercial()
    {
        // Arrange
        var path = "AIQ Urban::Building::FloorPlates::Commercial";

        // Act
        var result = LayerNameParser.ParseFloorPlateUse(path);

        // Assert
        Assert.That(result, Is.EqualTo("Commercial"));
    }

    [Test]
    public void ParseFloorPlateUse_WrongPrefix_ReturnsNull()
    {
        // Arrange
        var path = "AIQ Urban::Parcel::Residential";

        // Act
        var result = LayerNameParser.ParseFloorPlateUse(path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseFloorPlateUse_EmptyUseName_ReturnsNull()
    {
        // Arrange
        var path = "AIQ Urban::Building::FloorPlates::";

        // Act
        var result = LayerNameParser.ParseFloorPlateUse(path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseFloorPlateUse_NoSuffix_ReturnsNull()
    {
        // Arrange
        var path = "AIQ Urban::Building::FloorPlates";

        // Act
        var result = LayerNameParser.ParseFloorPlateUse(path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseParcelUse_Residential_ReturnsResidential()
    {
        // Arrange
        var path = "AIQ Urban::Parcel::Residential";

        // Act
        var result = LayerNameParser.ParseParcelUse(path);

        // Assert
        Assert.That(result, Is.EqualTo("Residential"));
    }

    [Test]
    public void ParseParcelUse_Park_ReturnsPark()
    {
        // Arrange
        var path = "AIQ Urban::Parcel::Park";

        // Act
        var result = LayerNameParser.ParseParcelUse(path);

        // Assert
        Assert.That(result, Is.EqualTo("Park"));
    }

    [Test]
    public void ParseParcelUse_WrongPrefix_ReturnsNull()
    {
        // Arrange
        var path = "AIQ Urban::Building::FloorPlates::Residential";

        // Act
        var result = LayerNameParser.ParseParcelUse(path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseParcelUse_EmptyUseName_ReturnsNull()
    {
        // Arrange
        var path = "AIQ Urban::Parcel::";

        // Act
        var result = LayerNameParser.ParseParcelUse(path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseZoneName_Downtown_ReturnsDowntown()
    {
        // Arrange
        var path = "AIQ Urban::Zone::Downtown";

        // Act
        var result = LayerNameParser.ParseZoneName(path);

        // Assert
        Assert.That(result, Is.EqualTo("Downtown"));
    }

    [Test]
    public void ParseZoneName_ParkZone_ReturnsParkZone()
    {
        // Arrange
        var path = "AIQ Urban::Zone::Park Zone";

        // Act
        var result = LayerNameParser.ParseZoneName(path);

        // Assert
        Assert.That(result, Is.EqualTo("Park Zone"));
    }

    [Test]
    public void ParseZoneName_WrongPrefix_ReturnsNull()
    {
        // Arrange
        var path = "AIQ Urban::Parcel::Downtown";

        // Act
        var result = LayerNameParser.ParseZoneName(path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseZoneName_EmptyName_ReturnsNull()
    {
        // Arrange
        var path = "AIQ Urban::Zone::";

        // Act
        var result = LayerNameParser.ParseZoneName(path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseZoneName_OnlyPrefix_ReturnsNull()
    {
        // Arrange
        var path = "AIQ Urban::Zone";

        // Act
        var result = LayerNameParser.ParseZoneName(path);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseTransitLayer_EmptyString_ReturnsNull()
    {
        // Act
        var result = LayerNameParser.ParseTransitLayer("");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseFloorPlateUse_EmptyString_ReturnsNull()
    {
        // Act
        var result = LayerNameParser.ParseFloorPlateUse("");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseParcelUse_EmptyString_ReturnsNull()
    {
        // Act
        var result = LayerNameParser.ParseParcelUse("");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseZoneName_EmptyString_ReturnsNull()
    {
        // Act
        var result = LayerNameParser.ParseZoneName("");

        // Assert
        Assert.That(result, Is.Null);
    }
}
