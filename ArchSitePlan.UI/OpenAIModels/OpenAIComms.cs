using System.Text.Json;
using System.Windows;
using OpenAI.Chat;

namespace ArchSitePlan.UI.OpenAI;

public class OpenAIComms
{

    static List<ChatMessage> _messages = [
        new UserChatMessage("What's the weather like today?"),
    ];

    static ChatCompletionOptions _options = new()
    {
        Tools = { getCurrentLocationTool, getCurrentWeatherTool },
        ToolChoice = ChatToolChoice.CreateRequiredChoice()
    };

    private const string accessKey = "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";

    public static void SendMessage()
    {
        ChatClient client = new(model: "gpt-4o", apiKey: accessKey);
        ChatCompletion completion = client.CompleteChat("Say 'this is a test.'");

        bool requiresAction;

        do
        {
            requiresAction = false;
            ChatCompletion chatCompletion = client.CompleteChat(_messages, _options);

            switch (chatCompletion.FinishReason)
            {
                case ChatFinishReason.Stop:
                    {
                        // Add the assistant message to the conversation history.
                        _messages.Add(new AssistantChatMessage(chatCompletion));
                        break;
                    }

                case ChatFinishReason.ToolCalls:
                    {
                        // First, add the assistant message with tool calls to the conversation history.
                        _messages.Add(new AssistantChatMessage(chatCompletion));

                        // Then, add a new tool message for each tool call that is resolved.
                        foreach (ChatToolCall toolCall in chatCompletion.ToolCalls)
                        {
                            switch (toolCall.FunctionName)
                            {
                                case nameof(GetCurrentLocation):
                                    {
                                        string toolResult = GetCurrentLocation();
                                        _messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                                        break;
                                    }

                                case nameof(GetCurrentWeather):
                                    {
                                        // The arguments that the model wants to use to call the function are specified as a
                                        // stringified JSON object based on the schema defined in the tool definition. Note that
                                        // the model may hallucinate arguments too. Consequently, it is important to do the
                                        // appropriate parsing and validation before calling the function.
                                        using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                        bool hasLocation = argumentsJson.RootElement.TryGetProperty("location", out JsonElement location);
                                        bool hasUnit = argumentsJson.RootElement.TryGetProperty("unit", out JsonElement unit);

                                        if (!hasLocation)
                                        {
                                            throw new ArgumentNullException(
                                                nameof(location), 
                                                "The location argument is required.");
                                        }

                                        string toolResult = hasUnit
                                            ? GetCurrentWeather(location.GetString(), unit.GetString())
                                            : GetCurrentWeather(location.GetString());
                                        _messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                                        break;
                                    }

                                default:
                                    {
                                        // Handle other unexpected calls.
                                        throw new NotImplementedException();
                                    }
                            }
                        }

                        requiresAction = true;
                        break;
                    }

                case ChatFinishReason.Length:
                    throw new NotImplementedException("Incomplete model output due to MaxTokens parameter or token limit exceeded.");

                case ChatFinishReason.ContentFilter:
                    throw new NotImplementedException("Omitted content due to a content filter flag.");

                case ChatFinishReason.FunctionCall:
                    throw new NotImplementedException("Deprecated in favor of tool calls.");

                default:
                    throw new NotImplementedException(chatCompletion.FinishReason.ToString());
            }
        } while (requiresAction);

        MessageBox.Show($"[ASSISTANT]: {completion.Content[0].Text}");
    }


    private static string GetCurrentLocation()
    {
        // Call the location API here.
        return "San Francisco";
    }

    private static string GetCurrentWeather(string location, string unit = "celsius")
    {
        // Call the weather API here.
        return $"31 {unit}";
    }

    private static readonly ChatTool getCurrentLocationTool = ChatTool.CreateFunctionTool(
    functionName: nameof(GetCurrentLocation),
    functionDescription: "Get the user's current location"
    );

    private static readonly ChatTool getCurrentWeatherTool = ChatTool.CreateFunctionTool(
    functionName: nameof(GetCurrentWeather),
    functionDescription: "Get the current weather in a given location",
    functionParameters: BinaryData.FromBytes("""
        {
            "type": "object",
            "properties": {
                "location": {
                    "type": "string",
                    "description": "The city and state, e.g. Boston, MA"
                },
                "unit": {
                    "type": "string",
                    "enum": [ "celsius", "fahrenheit" ],
                    "description": "The temperature unit to use. Infer this from the specified location."
                }
            },
            "required": [ "location" ]
        }
        """u8.ToArray())
);
}
