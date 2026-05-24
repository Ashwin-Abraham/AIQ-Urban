using System.Text.Json;
using AIQ.MCP.Models;

namespace AIQ.MCP.Services;

/// <summary>
/// Custom JSON serializer for element summaries, decoupled from any future persistence/export serialization.
/// Produces a stable JSON format optimised for AI consumption via the list_elements MCP tool.
/// </summary>
public static class ElementSummarySerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Serializes a ListElementsResponse to a JSON string for AI consumption.</summary>
    public static string Serialize(ListElementsResponse response)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

        writer.WriteStartObject();

        writer.WriteStartArray("transits");
        foreach (var t in response.Transits)
            WriteElement(writer, t);
        writer.WriteEndArray();

        writer.WriteStartArray("buildings");
        foreach (var b in response.Buildings)
            WriteElement(writer, b);
        writer.WriteEndArray();

        writer.WriteStartArray("parcels");
        foreach (var p in response.Parcels)
            WriteElement(writer, p);
        writer.WriteEndArray();

        writer.WriteStartArray("zones");
        foreach (var z in response.Zones)
            WriteElement(writer, z);
        writer.WriteEndArray();

        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteElement(Utf8JsonWriter writer, ElementSummary element)
    {
        writer.WriteStartObject();

        writer.WriteString("id", element.Id.ToString());
        writer.WriteString("type", element.Type);

        if (element.Use is not null)
            writer.WriteString("use", element.Use);
        else
            writer.WriteNull("use");

        writer.WriteString("summary", element.Summary);

        if (element.Details is { Count: > 0 })
        {
            writer.WriteStartObject("details");
            foreach (var kvp in element.Details)
            {
                writer.WritePropertyName(kvp.Key);
                WriteValue(writer, kvp.Value);
            }
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case Guid g:
                writer.WriteStringValue(g.ToString());
                break;
            case double[] arr:
                writer.WriteStartArray();
                foreach (var v in arr)
                    writer.WriteNumberValue(v);
                writer.WriteEndArray();
                break;
            case int[] arr:
                writer.WriteStartArray();
                foreach (var v in arr)
                    writer.WriteNumberValue(v);
                writer.WriteEndArray();
                break;
            case string[] arr:
                writer.WriteStartArray();
                foreach (var v in arr)
                    writer.WriteStringValue(v);
                writer.WriteEndArray();
                break;
            case System.Collections.IEnumerable enumerable:
                writer.WriteStartArray();
                foreach (var item in enumerable)
                    WriteValue(writer, item);
                writer.WriteEndArray();
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}
