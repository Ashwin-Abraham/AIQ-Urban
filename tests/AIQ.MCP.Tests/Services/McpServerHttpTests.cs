using System.Net;
using System.Text.Json;
using AIQ.MCP.Services;
using NUnit.Framework;

namespace AIQ.MCP.Tests.Services;

[TestFixture]
public class McpServerHttpTests
{
    private McpServer _server;

    [SetUp]
    public void SetUp()
    {
        _server = new McpServer();
    }

    [TearDown]
    public void TearDown()
    {
        if (_server?.IsRunning == true)
            _server.StopAsync().GetAwaiter().GetResult();
        _server?.Dispose();
        var portFile = Path.Combine(Path.GetTempPath(), "AIQ.MCP.port");
        try { if (File.Exists(portFile)) File.Delete(portFile); } catch { }
    }

    [Test]
    public void Initialize_ReturnsServerInfo()
    {
        _server.Start(0).GetAwaiter().GetResult();
        var port = _server.Port;

        var request = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new { name = "test-client", version = "1.0.0" }
            }
        };
        var json = JsonSerializer.Serialize(request);
        using var client = new HttpClient();

        var response = Task.Run(() =>
            client.PostAsync($"http://localhost:{port}/",
                new StringContent(json, System.Text.Encoding.UTF8, "application/json")))
            .GetAwaiter().GetResult();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = Task.Run(() => response.Content.ReadAsStringAsync()).GetAwaiter().GetResult();
        Assert.That(body, Does.Contain("event: message"));
        Assert.That(body, Does.Contain("\"name\":\"AIQ.MCP\""));
        Assert.That(body, Does.Contain("\"version\":\"1.0.0\""));
    }

    [Test]
    public void UnknownMethod_ReturnsError()
    {
        _server.Start(0).GetAwaiter().GetResult();
        var port = _server.Port;

        var request = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "nonexistent_method",
            @params = new { }
        };
        var json = JsonSerializer.Serialize(request);
        using var client = new HttpClient();

        var response = Task.Run(() =>
            client.PostAsync($"http://localhost:{port}/",
                new StringContent(json, System.Text.Encoding.UTF8, "application/json")))
            .GetAwaiter().GetResult();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = Task.Run(() => response.Content.ReadAsStringAsync()).GetAwaiter().GetResult();
        Assert.That(body, Does.Contain("event: message"));
        Assert.That(body, Does.Contain("\"code\":-32601"));
    }

    [Test]
    public void MalformedJson_Returns500()
    {
        _server.Start(0).GetAwaiter().GetResult();
        var port = _server.Port;

        using var client = new HttpClient();
        var response = Task.Run(() =>
            client.PostAsync($"http://localhost:{port}/",
                new StringContent("not valid json", System.Text.Encoding.UTF8, "application/json")))
            .GetAwaiter().GetResult();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        var body = Task.Run(() => response.Content.ReadAsStringAsync()).GetAwaiter().GetResult();
        Assert.That(body, Does.Contain("error"));
    }

    [Test]
    public void Notification_WithoutId_Returns202()
    {
        _server.Start(0).GetAwaiter().GetResult();
        var port = _server.Port;

        var request = new
        {
            jsonrpc = "2.0",
            method = "notifications/test",
            @params = new { }
        };
        var json = JsonSerializer.Serialize(request);
        using var client = new HttpClient();

        var response = Task.Run(() =>
            client.PostAsync($"http://localhost:{port}/",
                new StringContent(json, System.Text.Encoding.UTF8, "application/json")))
            .GetAwaiter().GetResult();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
    }

    [Test]
    public void Initialize_Response_ContainsServerCapabilities()
    {
        _server.Start(0).GetAwaiter().GetResult();
        var port = _server.Port;

        var request = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new { name = "test-client", version = "1.0.0" }
            }
        };
        var json = JsonSerializer.Serialize(request);
        using var client = new HttpClient();

        var response = Task.Run(() =>
            client.PostAsync($"http://localhost:{port}/",
                new StringContent(json, System.Text.Encoding.UTF8, "application/json")))
            .GetAwaiter().GetResult();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = Task.Run(() => response.Content.ReadAsStringAsync()).GetAwaiter().GetResult();
        Assert.That(body, Does.Contain("protocolVersion"));
        Assert.That(body, Does.Contain("\"tools\""));
    }

}
