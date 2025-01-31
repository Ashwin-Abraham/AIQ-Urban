using ArchSitePlan.UI.HTML_UI.MessagesFromUI;
using ArchSitePlan.UI.HTML_UI.MessageToUI;
using Newtonsoft.Json;

namespace ArchSitePlan.UI.HTML_UI.HtmlCommsClasses;

public static class MessageConverter
{
    const string _messageTypeKey = "messageType";

    public static IMessageFromUI<IMessageToUI> ConvertMessageToJson(string json)
    {
        // Deserialize the JSON into a generic object first to get the messageType
        var tempObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

        if (tempObj == null || !tempObj.ContainsKey(_messageTypeKey))
            throw new Exception($"Invalid JSON: No '{_messageTypeKey}' found.");

        var messageType = tempObj[_messageTypeKey].ToString();

        // Deserialize into the appropriate IMessage type based on the messageType field
        switch (messageType)
        {
            case "Transit":
                return JsonConvert.DeserializeObject<TransitMessage>(json);

            case "Landuse":
                return JsonConvert.DeserializeObject<LanduseMessage>(json);

            case "Building":
                return JsonConvert.DeserializeObject<BuildingMessage>(json);

            case "Divider":
                return JsonConvert.DeserializeObject<DividerMessage>(json);

            case "Zone":
                return JsonConvert.DeserializeObject<ZoneMessage>(json);

            case "ToolSelection":
                return JsonConvert.DeserializeObject<ToolSelectionMessage>(json);

            default:
                throw new Exception($"Unsupported message type: {messageType}");
        }
    }
}
