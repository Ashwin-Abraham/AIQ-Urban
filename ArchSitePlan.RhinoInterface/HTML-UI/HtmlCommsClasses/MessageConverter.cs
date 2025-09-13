using ArchSitePlan.RhinoInterface.HTML_UI.MessagesFromUI;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Newtonsoft.Json;

namespace ArchSitePlan.RhinoInterface.HTML_UI.HtmlCommsClasses;

public static class MessageConverter
{
    const string _messageTypeKey = "messageType";

    public static IMessageFromUI<IMessageToUI> ConvertMessageToJson(string json)
    {
        // Deserialize the JSON into a generic object first to get the messageType
        var tempObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

        if (tempObj == null || !tempObj.ContainsKey(_messageTypeKey))
            throw new Exception($"Invalid JSON: No '{_messageTypeKey}' found.");

        var messageType = tempObj[_messageTypeKey].ToString().ToLower();

        // Deserialize into the appropriate IMessage type based on the messageType field
        switch (messageType)
        {
            case "transit":
                return JsonConvert.DeserializeObject<TransitMessage>(json);

            case "landuse":
                return JsonConvert.DeserializeObject<LanduseMessage>(json);

            case "building":
                return JsonConvert.DeserializeObject<BuildingMessage>(json);

            case "divider":
                return JsonConvert.DeserializeObject<DividerMessage>(json);

            case "zone":
                return JsonConvert.DeserializeObject<ZoneMessage>(json);

            case "toolselection":
                return JsonConvert.DeserializeObject<ToolSelectionMessage>(json);

            case "landuselistedit":
                return JsonConvert.DeserializeObject<LanduseListEditMessage>(json);

            case "pickfile":
                return JsonConvert.DeserializeObject<PickFileMessage>(json);

            case "readosm":
                return JsonConvert.DeserializeObject<ReadOsmMessage>(json);

            case "autobuilding":
                return JsonConvert.DeserializeObject<AutoBuildingMessage>(json);

            case "exportexcelpathselection":
                return JsonConvert.DeserializeObject<ExportExcelPathSelection>(json);

            case "exportexcel":
                return JsonConvert.DeserializeObject<ExportExcel>(json);

            case "undo":
                return JsonConvert.DeserializeObject<UndoMessage>(json);

            case "redo":
                return JsonConvert.DeserializeObject<RedoMessage>(json);

            default:
                throw new Exception($"Unsupported message type: {messageType}");
        }
    }
}
