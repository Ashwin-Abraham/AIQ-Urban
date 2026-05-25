namespace AIQ.MCP.Services;

public class McpServer
{
    public async Task Start(int preferredPort = 0)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services
            .AddMcpServer()
            .WithTools<UrbanModelTools>();

        var app = builder.Build();

        app.MapMcp("/mcp");

        await app.RunAsync();
    }

}
