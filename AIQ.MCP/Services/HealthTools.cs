using ModelContextProtocol.Server;

namespace AIQ.MCP.Services;

[McpServerToolType]
public class HealthTools
{
    [McpServerTool(Name = "test_health")]
    public static string TestHealth() => "Your AIQ MCP Server is HEALTHY";
}
