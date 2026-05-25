using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace AIQ.MCP.Services;

public class McpServer : IAsyncDisposable
{
    private WebApplication? _app;

    public int? Port { get; private set; }

    public async Task Start(int preferredPort = 0)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseUrls($"http://localhost:{preferredPort}");

        builder.Services
            .AddMcpServer()
            .WithTools<UrbanModelTools>();

        _app = builder.Build();

        _app.MapMcp("/mcp");

        await _app.StartAsync();

        var address = _app.Urls.FirstOrDefault() ?? "http://localhost:0";
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
