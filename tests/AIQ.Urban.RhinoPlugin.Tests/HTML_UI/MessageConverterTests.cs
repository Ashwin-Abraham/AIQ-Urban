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
        const string json = "{\"messageType\":\"ExportExcel\",\"filePath\":\"C:\\\\Exports\\\\Scripted.xlsx\"}";

        var message = MessageConverter.Convert(json);

        Assert.That(message, Is.TypeOf<ExportExcelMessage>());
        Assert.That(((ExportExcelMessage)message).FilePath, Is.EqualTo(@"C:\Exports\Scripted.xlsx"));
    }

    [Test]
    public void Convert_ZoneMessage_ReadsObjectIds()
    {
        const string id = "6634e8c5-cea3-479e-839d-430a3a32ff32";
        var json = "{\"messageType\":\"Zone\",\"name\":\"Downtown\",\"objectIds\":[\"" + id + "\"]}";

        var message = MessageConverter.Convert(json);

        Assert.That(message, Is.TypeOf<AddZoneMessage>());
        Assert.That(((AddZoneMessage)message).ObjectIds, Is.EqualTo(new[] { System.Guid.Parse(id) }));
    }
}
