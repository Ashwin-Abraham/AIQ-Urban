using AIQ.Urban.RhinoPlugin.HTML_UI;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;
using NUnit.Framework;

namespace AIQ.Urban.RhinoInterface.Tests;

public class MessageConverterTests
{
    [Test]
    public void Convert_DividerMessage_ReturnsAddParcelBoundaryMessage()
    {
        // Arrange
        const string json = "{\"messageType\":\"Divider\"}";

        // Act
        var message = MessageConverter.Convert(json);

        // Preview
        TestContext.WriteLine($"Converted message type: {message.GetType().Name}");

        // Assert
        Assert.That(message, Is.TypeOf<AddParcelBoundaryMessage>());
    }

    [Test]
    public void Convert_ZoneMessage_ReturnsAddZoneMessage()
    {
        // Arrange
        const string json = "{\"messageType\":\"Zone\",\"name\":\"Downtown\"}";

        // Act
        var message = MessageConverter.Convert(json);

        // Preview
        TestContext.WriteLine($"Converted message type: {message.GetType().Name}");

        // Assert
        Assert.That(message, Is.TypeOf<AddZoneMessage>());
    }

    [Test]
    public void Convert_LanduseMessage_ReturnsChangeUseMessage()
    {
        // Arrange
        const string json = "{\"messageType\":\"Landuse\",\"use\":\"Residential\"}";

        // Act
        var message = MessageConverter.Convert(json);

        // Preview
        TestContext.WriteLine($"Converted message type: {message.GetType().Name}");

        // Assert
        Assert.That(message, Is.TypeOf<ChangeUseMessage>());
    }

    [Test]
    public void Convert_GetInstallationIdMessage_ReturnsGetInstallationIdMessage()
    {
        // Arrange
        const string json = "{\"messageType\":\"GetInstallationId\"}";

        // Act
        var message = MessageConverter.Convert(json);

        // Preview
        TestContext.WriteLine($"Converted message type: {message.GetType().Name}");

        // Assert
        Assert.That(message, Is.TypeOf<GetInstallationIdMessage>());
    }

    [Test]
    public void Convert_ExportExcelMessage_ReturnsExportExcelMessage()
    {
        const string json = "{\"messageType\":\"ExportExcel\"}";

        var message = MessageConverter.Convert(json);

        Assert.That(message, Is.TypeOf<ExportExcelMessage>());
    }
}
