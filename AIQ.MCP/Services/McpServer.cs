using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace AIQ.MCP.Services;

public class McpServerHost : IAsyncDisposable
{
    private WebApplication? _app;
    public int? Port { get; private set; }

    private McpRequestHandler<ListToolsRequestParams, ListToolsResult>? _listToolsHandler;
    private McpRequestHandler<CallToolRequestParams, CallToolResult>? _callToolHandler;

    public void OnListTools(Func<RequestContext<ListToolsRequestParams>, CancellationToken, ValueTask<ListToolsResult>> handler)
    {
        _listToolsHandler = new McpRequestHandler<ListToolsRequestParams, ListToolsResult>(handler);
    }

    public void OnCallTool(Func<RequestContext<CallToolRequestParams>, CancellationToken, ValueTask<CallToolResult>> handler)
    {
        _callToolHandler = new McpRequestHandler<CallToolRequestParams, CallToolResult>(handler);
    }

    public async Task Start(int preferredPort = 0)
    {
        var builder = WebApplication.CreateBuilder();

        var listenUrl = preferredPort == 0 ? "http://127.0.0.1:0" : $"http://127.0.0.1:{preferredPort}";
        builder.WebHost.UseUrls(listenUrl);

        var mcpBuilder = builder.Services.AddMcpServer();
        mcpBuilder.WithHttpTransport();
        mcpBuilder.WithTools<HealthTools>();

        if (_listToolsHandler is not null)
            mcpBuilder.WithListToolsHandler(_listToolsHandler);

        if (_callToolHandler is not null)
            mcpBuilder.WithCallToolHandler(_callToolHandler);

        _app = builder.Build();
        _app.MapMcp("/mcp");

        await _app.StartAsync();

        var address = _app.Urls.FirstOrDefault() ?? "http://127.0.0.1:0";
        Port = int.Parse(address.Split(':').Last());
    }

    public async Task Stop()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
            _app = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Stop();
    }
}
