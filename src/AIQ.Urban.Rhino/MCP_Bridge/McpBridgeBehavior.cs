using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;
using System.Text.Json.Nodes;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;
using Rhino;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace AIQ.Urban.RhinoPlugin.MCP_Bridge;

internal class McpBridgeBehavior : WebSocketBehavior
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Lazy<Dictionary<string, ToolMethod>> _toolMethods = new(DiscoverToolMethods);

    private record ToolMethod(MethodInfo Method, McpToolAttribute Attribute, ParameterInfo[] Parameters);

    protected override void OnMessage(MessageEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.Data);
            var root = doc.RootElement;

            var type = root.TryGetProperty("type", out var t) ? t.GetString() : null;
            var requestId = root.TryGetProperty("requestId", out var r) ? r.GetString() : null;

            switch (type)
            {
                case "ListTools":
                    HandleListTools(requestId);
                    break;

                case "ToolCall":
                    var toolName = root.TryGetProperty("tool", out var tn) ? tn.GetString() : null;
                    var arguments = root.TryGetProperty("arguments", out var args) ? args : (JsonElement?)null;
                    HandleToolCall(requestId, toolName, arguments);
                    break;
            }
        }
        catch (Exception ex)
        {
            SendError(null, $"Bridge error: {ex.Message}");
        }
    }

    private void HandleListTools(string? requestId)
    {
        var tools = new JsonArray();

        foreach (var (name, toolMethod) in _toolMethods.Value)
        {
            var props = new JsonObject();
            var required = new List<string>();

            foreach (var param in toolMethod.Parameters)
            {
                var paramName = param.Name!;
                props[paramName] = BuildParameterSchema(param);

                if (!param.HasDefaultValue)
                    required.Add(paramName);
            }

            var inputSchema = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = props,
                ["required"] = new JsonArray(required.Select(r => JsonValue.Create(r)).ToArray())
            };

            tools.Add(new JsonObject
            {
                ["name"] = name,
                ["description"] = toolMethod.Attribute.Description,
                ["inputSchema"] = inputSchema
            });
        }

        var response = new JsonObject
        {
            ["type"] = "ToolsList",
            ["requestId"] = requestId,
            ["tools"] = tools
        };

        Send(response.ToJsonString());
    }

    private void HandleToolCall(string? requestId, string? toolName, JsonElement? arguments)
    {
        if (string.IsNullOrEmpty(toolName))
        {
            SendError(requestId, "Tool name is required");
            return;
        }

        if (!_toolMethods.Value.TryGetValue(toolName, out var toolMethod))
        {
            SendError(requestId, $"Unknown tool: {toolName}");
            return;
        }

        try
        {
            var rhinoDoc = RhinoDoc.ActiveDoc;
            if (rhinoDoc is null)
            {
                SendError(requestId, "No active Rhino document");
                return;
            }

            var model = UrbanModel.Instance;
            var toolsInstance = new UrbanModelTools(rhinoDoc, model);
            var args = arguments ?? JsonDocument.Parse("{}").RootElement;
            var paramValues = new object?[toolMethod.Parameters.Length];

            for (int i = 0; i < toolMethod.Parameters.Length; i++)
            {
                var param = toolMethod.Parameters[i];

                if (args.TryGetProperty(param.Name!, out var paramValue))
                {
                    paramValues[i] = ConvertParamValue(paramValue, param.ParameterType);
                }
                else if (param.HasDefaultValue)
                {
                    paramValues[i] = param.DefaultValue;
                }
                else
                {
                    SendError(requestId, $"Missing required parameter: {param.Name}");
                    return;
                }
            }

            var result = toolMethod.Method.Invoke(toolsInstance, paramValues);
            var resultText = result?.ToString() ?? "";

            var response = new JsonObject
            {
                ["type"] = "ToolResult",
                ["requestId"] = requestId,
                ["content"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = resultText
                    }
                }
            };

            Send(response.ToJsonString());
        }
        catch (Exception ex)
        {
            SendError(requestId, $"Tool execution error: {ex.Message}");
        }
    }

    private static Dictionary<string, ToolMethod> DiscoverToolMethods()
    {
        var methods = typeof(UrbanModelTools)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<McpToolAttribute>() is not null);

        var result = new Dictionary<string, ToolMethod>();

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<McpToolAttribute>()!;
            var parameters = method.GetParameters();
            result[attr.Name] = new ToolMethod(method, attr, parameters);
        }

        return result;
    }

    private static JsonNode BuildParameterSchema(ParameterInfo param)
    {
        var schema = BuildTypeSchema(param.ParameterType);

        if (param.ParameterType == typeof(string)
            && string.Equals(param.Name, "useName", StringComparison.OrdinalIgnoreCase))
        {
            schema["enum"] = new JsonArray(
                UrbanModel.Instance.Uses.Select(u => JsonValue.Create(u.Name)).ToArray());
        }

        return schema;
    }

    private static JsonNode BuildTypeSchema(Type type)
    {
        if (type == typeof(double))
            return new JsonObject { ["type"] = "number" };

        if (type == typeof(int))
            return new JsonObject { ["type"] = "integer" };

        if (type == typeof(string))
            return new JsonObject { ["type"] = "string" };

        if (type == typeof(bool))
            return new JsonObject { ["type"] = "boolean" };

        if (type.IsEnum)
        {
            var names = Enum.GetNames(type);
            return new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray(names.Select(n => JsonValue.Create(n)).ToArray())
            };
        }

        if (type == typeof(Guid))
            return new JsonObject { ["type"] = "string" };

        if (type == typeof(Point2d))
            return new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["x"] = new JsonObject { ["type"] = "number" },
                    ["y"] = new JsonObject { ["type"] = "number" }
                },
                ["required"] = new JsonArray("x", "y")
            };

        if (type == typeof(CurveCoordinates))
            return new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["points"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = BuildTypeSchema(typeof(Point2d))
                    }
                },
                ["required"] = new JsonArray("points")
            };

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = type.GetGenericArguments()[0];
            return new JsonObject
            {
                ["type"] = "array",
                ["items"] = BuildTypeSchema(elementType)
            };
        }

        return new JsonObject { ["type"] = "string" };
    }

    private static object? ConvertParamValue(JsonElement element, Type targetType)
    {
        if (element.ValueKind == JsonValueKind.Null)
            return null;

        if (targetType == typeof(double)) return element.GetDouble();
        if (targetType == typeof(int)) return element.GetInt32();
        if (targetType == typeof(string)) return element.GetString() ?? "";
        if (targetType == typeof(bool)) return element.GetBoolean();
        if (targetType == typeof(Guid)) return Guid.Parse(element.GetString()!);

        if (targetType == typeof(TransitType))
            return Enum.Parse<TransitType>(element.GetString()!, ignoreCase: true);

        if (targetType == typeof(Point2d))
            return ParsePoint2d(element);

        if (targetType == typeof(CurveCoordinates))
            return ParseCurveCoordinates(element);

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = targetType.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (System.Collections.IList)Activator.CreateInstance(listType)!;

            foreach (var item in element.EnumerateArray())
                list.Add(ConvertParamValue(item, elementType));

            return list;
        }

        throw new ArgumentException($"Cannot convert JSON element to {targetType.Name}");
    }

    private static Point2d ParsePoint2d(JsonElement element)
    {
        return new Point2d(
            element.GetProperty("x").GetDouble(),
            element.GetProperty("y").GetDouble());
    }

    private static CurveCoordinates ParseCurveCoordinates(JsonElement element)
    {
        var points = new List<Point2d>();
        foreach (var pt in element.GetProperty("points").EnumerateArray())
            points.Add(ParsePoint2d(pt));

        return new CurveCoordinates(points);
    }

    private void SendError(string? requestId, string message)
    {
        var response = new JsonObject
        {
            ["type"] = "Error",
            ["requestId"] = requestId,
            ["error"] = message
        };
        Send(response.ToJsonString());
    }
}
