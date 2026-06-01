using System.Text.Json;
using AIQ.MCP.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

const int Port = 6410;

var bridgeUrl = ParseArgs(args);
var portFile = Path.Combine(Path.GetTempPath(), "AIQ.MCP.port");
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    var server = new McpServerHost();

    if (bridgeUrl is not null)
    {
        var bridge = new BridgeClient(bridgeUrl);
        await bridge.ConnectAsync(cts.Token).ConfigureAwait(false);

        Console.WriteLine($"Connected to Rhino bridge at {bridgeUrl}");

        var toolDefs = await bridge.ListToolsAsync(cts.Token).ConfigureAwait(false);
        Console.WriteLine($"Discovered {toolDefs.Count} MCP tools from Rhino");

        server.OnListTools(async (_, ct) =>
        {
            var toolList = toolDefs.Select(t => new Tool
            {
                Name = t.Name,
                Description = t.Description ?? string.Empty,
                InputSchema = t.InputSchema
            }).ToList();

            return new ListToolsResult { Tools = toolList };
        });

        server.OnCallTool(async (request, ct) =>
        {
            try
            {
                var argsElement = request.Params.Arguments is not null
                    ? JsonSerializer.SerializeToElement(request.Params.Arguments)
                    : JsonDocument.Parse("{}").RootElement;

                var result = await bridge.CallToolAsync(
                    request.Params.Name,
                    argsElement,
                    ct).ConfigureAwait(false);

                return new CallToolResult
                {
                    Content = [new TextContentBlock { Text = result.GetRawText() }]
                };
            }
            catch (Exception ex)
            {
                return new CallToolResult
                {
                    Content = [new TextContentBlock { Text = $"Error: {ex.Message}" }],
                    IsError = true
                };
            }
        });
    }
    else
    {
        Console.WriteLine("Running in standalone mode (no Rhino bridge)");
    }

    await server.Start(Port).ConfigureAwait(false);

    await File.WriteAllTextAsync(portFile, $"http://localhost:{Port}/mcp", cts.Token).ConfigureAwait(false);
    Console.WriteLine($"MCP server started on http://localhost:{Port}/mcp");
    Console.WriteLine($"Port file: {portFile}");

    try
    {
        await Task.Delay(-1, cts.Token).ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
    }
}
catch (OperationCanceledException)
{
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal error: {ex.Message}");
    return 1;
}
finally
{
    try { if (File.Exists(portFile)) File.Delete(portFile); } catch { }
}

return 0;

static string? ParseArgs(string[] args)
{
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--bridge" && i + 1 < args.Length)
            return args[i + 1];
    }
    return null;
}
