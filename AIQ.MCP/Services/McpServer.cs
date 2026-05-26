using AIQ.Urban.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Rhino;

namespace AIQ.MCP.Services;

public class McpServer(RhinoDoc doc, UrbanModel model) : IAsyncDisposable
{
    private WebApplication? _app;

    public int? Port { get; private set; }

    public async Task Start(int preferredPort = 0)
    {
        var builder = WebApplication.CreateBuilder();

        var listenUrl = preferredPort == 0 ? "http://127.0.0.1:0" : $"http://127.0.0.1:{preferredPort}";
        builder.WebHost.UseUrls(listenUrl);

        var tools = new UrbanModelTools(doc, model);
        builder.Services
            .AddMcpServer()
            .WithTools(tools)
            .WithHttpTransport();

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
