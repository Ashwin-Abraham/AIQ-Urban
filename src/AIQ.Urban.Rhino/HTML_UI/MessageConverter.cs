using System;
using System.Text.Json;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;

namespace AIQ.Urban.RhinoPlugin.HTML_UI;

public static class MessageConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    public static IMessageFromUI<IMessageToUI> Convert(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var messageType = doc.RootElement.GetProperty("messageType").GetString()?.ToLower();

        return messageType switch
        {
            "transit" => Deserialize<TransitMessage>(json),
            "building" => Deserialize<BuildingMessage>(json),
            "zone" => Deserialize<AddZoneMessage>(json),
            "landuse" => Deserialize<ChangeUseMessage>(json),
            "changebuildinguse" => Deserialize<ChangeBuildingUseMessage>(json),
            "toolselection" => new ToolSelectionMessage(),
            _ => throw new Exception($"Unsupported messageType: {messageType}")
        };
    }

    private static T Deserialize<T>(string json) where T : class
    {
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new Exception($"Failed to deserialize {typeof(T).Name} from JSON.");
    }
}
