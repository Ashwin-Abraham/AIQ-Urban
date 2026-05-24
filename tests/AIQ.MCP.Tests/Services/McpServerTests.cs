using System.Net;
using System.Text.Json;
using AIQ.MCP.Services;
using NUnit.Framework;

namespace AIQ.MCP.Tests.Services;

public class McpServerTests
{
    private readonly string _portFilePath = Path.Combine(Path.GetTempPath(), "AIQ.MCP.port");

    [TearDown]
    public void TearDown()
    {
        // Clean up any leftover server and port file
        try { if (File.Exists(_portFilePath)) File.Delete(_portFilePath); } catch { }
    }

    [Test]
    public void Start_BindsToDynamicPort()
    {
        using var server = new McpServer();
        server.Start(0).GetAwaiter().GetResult();

        Assert.That(server.IsRunning, Is.True);
        Assert.That(server.Port, Is.GreaterThan(0));

        server.DisposeAsync().GetAwaiter().GetResult();
    }

    [Test]
    public void Start_WritesPortFile()
    {
        try { if (File.Exists(_portFilePath)) File.Delete(_portFilePath); } catch { }

        using var server = new McpServer();
        server.Start(0).GetAwaiter().GetResult();

        Assert.That(File.Exists(_portFilePath), Is.True);
        var content = File.ReadAllText(_portFilePath);
        Assert.That(content, Is.EqualTo(server.Port.ToString()));

        server.DisposeAsync().GetAwaiter().GetResult();
    }

    [Test]
    public void Stop_DeletesPortFile()
    {
        using var server = new McpServer();
        server.Start(0).GetAwaiter().GetResult();
        Assert.That(File.Exists(_portFilePath), Is.True);

        server.StopAsync().GetAwaiter().GetResult();

        Assert.That(File.Exists(_portFilePath), Is.False);
        Assert.That(server.IsRunning, Is.False);
    }

    [Test]
    public void Stop_Idempotent()
    {
        using var server = new McpServer();
        server.Start(0).GetAwaiter().GetResult();

        server.StopAsync().GetAwaiter().GetResult();
        server.StopAsync().GetAwaiter().GetResult();

        Assert.That(server.IsRunning, Is.False);
    }

    [Test]
    public void StartStop_Restart_Succeeds()
    {
        using var server = new McpServer();
        server.Start(0).GetAwaiter().GetResult();
        var firstPort = server.Port;
        server.StopAsync().GetAwaiter().GetResult();

        server.Start(0).GetAwaiter().GetResult();

        Assert.That(server.IsRunning, Is.True);
        Assert.That(server.Port, Is.GreaterThan(0));
    }
}
