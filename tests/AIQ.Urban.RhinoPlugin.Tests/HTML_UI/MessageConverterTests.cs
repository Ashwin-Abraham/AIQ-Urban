using AIQ.Urban.RhinoPlugin.HTML_UI;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;
using NUnit.Framework;

namespace AIQ.Urban.RhinoInterface.Tests;

public class MessageConverterTests
{
    [Test]
    public void Convert_ZoneMessage_ReturnsAddZoneMessage()
    {
        var message = MessageConverter.Convert("{\"messageType\":\"Zone\",\"name\":\"Downtown\"}");

        Assert.That(message, Is.TypeOf<AddZoneMessage>());
    }

    [Test]
    public void Convert_LanduseMessage_ReturnsChangeParcelUseMessage()
    {
        var message = MessageConverter.Convert("{\"messageType\":\"Landuse\",\"use\":\"Residential\"}");

        Assert.That(message, Is.TypeOf<ChangeParcelUseMessage>());
    }

    [Test]
    public void Convert_ChangeBuildingUseMessage_ReturnsChangeBuildingUseMessage()
    {
        var message = MessageConverter.Convert("{\"messageType\":\"ChangeBuildingUse\",\"useName\":\"Commercial\"}");

        Assert.That(message, Is.TypeOf<ChangeBuildingUseMessage>());
    }
}
