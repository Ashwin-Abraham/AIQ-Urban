using System.Text.Json;

namespace AIQ.MCP;

internal record BridgeEnvelope(
    string Type,
    string? RequestId,
    string? Tool,
    JsonElement? Arguments,
    JsonElement? Tools,
    JsonElement? Content,
    string? Error);

internal record ToolDefinition(
    string Name,
    string? Description,
    JsonElement InputSchema);
