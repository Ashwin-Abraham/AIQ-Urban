using System.Net;
using System.Text.Json;
using AIQ.MCP.Services;
using AIQ.Urban.Core;
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

    [Test]
    public void ListTools_AfterInitialize_ContainsListElements()
    {
        // Arrange
        UrbanModel.Instance.Reset();
        _server.Start(0).GetAwaiter().GetResult();
        var port = _server.Port;

        // First initialize
        var initRequest = new
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

        using var client = new HttpClient();
        var initJson = JsonSerializer.Serialize(initRequest);
        var initResponse = Task.Run(() =>
            client.PostAsync($"http://localhost:{port}/",
                new StringContent(initJson, System.Text.Encoding.UTF8, "application/json")))
            .GetAwaiter().GetResult();
        Assert.That(initResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Act — list tools
        var listRequest = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/list",
            @params = new { }
        };
        var listJson = JsonSerializer.Serialize(listRequest);
        var listResponse = Task.Run(() =>
            client.PostAsync($"http://localhost:{port}/",
                new StringContent(listJson, System.Text.Encoding.UTF8, "application/json")))
            .GetAwaiter().GetResult();

        // Assert
        Assert.That(listResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var listBody = Task.Run(() => listResponse.Content.ReadAsStringAsync()).GetAwaiter().GetResult();
        Assert.That(listBody, Does.Contain("list_elements"));
        Assert.That(listBody, Does.Contain("Lists all urban elements"));
    }

    [Test]
    public void ListElements_EmptyModel_ReturnsEmptyLists()
    {
        // Arrange
        UrbanModel.Instance.Reset();
        _server.Start(0).GetAwaiter().GetResult();
        var port = _server.Port;

        // Initialize + list elements in one call via tools/call
        var initRequest = new
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

        using var client = new HttpClient();
        var initJson = JsonSerializer.Serialize(initRequest);
        var initResponse = Task.Run(() =>
            client.PostAsync($"http://localhost:{port}/",
                new StringContent(initJson, System.Text.Encoding.UTF8, "application/json")))
            .GetAwaiter().GetResult();
        Assert.That(initResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Act — call list_elements
        var callRequest = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new
            {
                name = "list_elements",
                arguments = new { }
            }
        };
        var callJson = JsonSerializer.Serialize(callRequest);
        var callResponse = Task.Run(() =>
            client.PostAsync($"http://localhost:{port}/",
                new StringContent(callJson, System.Text.Encoding.UTF8, "application/json")))
            .GetAwaiter().GetResult();

        // Assert
        Assert.That(callResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = Task.Run(() => callResponse.Content.ReadAsStringAsync()).GetAwaiter().GetResult();
        // The response contains escaped JSON within the text content block
        Assert.That(body, Does.Contain("transits"));
        Assert.That(body, Does.Contain("buildings"));
        Assert.That(body, Does.Contain("parcels"));
        Assert.That(body, Does.Contain("zones"));
        // Verify it's valid JSON with empty arrays (quotes may be escaped as \u0022 within the JSON string)
        Assert.That(body, Does.Contain("\\u0022transits\\u0022:[]"));
        Assert.That(body, Does.Contain("\\u0022buildings\\u0022:[]"));
        Assert.That(body, Does.Contain("\\u0022parcels\\u0022:[]"));
        Assert.That(body, Does.Contain("\\u0022zones\\u0022:[]"));
    }

    [Test]
    public void CallTool_UnknownTool_ReturnsError()
    {
        _server.Start(0).GetAwaiter().GetResult();
        var port = _server.Port;

        var initRequest = new
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

        using var client = new HttpClient();
        var initJson = JsonSerializer.Serialize(initRequest);
        var initResponse = Task.Run(() =>
            client.PostAsync($"http://localhost:{port}/",
                new StringContent(initJson, System.Text.Encoding.UTF8, "application/json")))
            .GetAwaiter().GetResult();
        Assert.That(initResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Act — call nonexistent tool
        var callRequest = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new
            {
                name = "nonexistent_tool",
                arguments = new { }
            }
        };
        var callJson = JsonSerializer.Serialize(callRequest);
        var callResponse = Task.Run(() =>
            client.PostAsync($"http://localhost:{port}/",
                new StringContent(callJson, System.Text.Encoding.UTF8, "application/json")))
            .GetAwaiter().GetResult();

        // Assert — should receive error
        Assert.That(callResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = Task.Run(() => callResponse.Content.ReadAsStringAsync()).GetAwaiter().GetResult();
        Assert.That(body, Does.Contain("\"code\":-32602"));
    }

    [Test]
    public void ListTools_AfterInitialize_ContainsAddBuilding()
    {
        // Arrange
        UrbanModel.Instance.Reset();
        _server.Start(0).GetAwaiter().GetResult();
        var port = _server.Port;

        // First initialize
        var initRequest = new
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

        using var client = new HttpClient();
        var initJson = JsonSerializer.Serialize(initRequest);
        var initResponse = Task.Run(() =>
            client.PostAsync($"http://localhost:{port}/",
                new StringContent(initJson, System.Text.Encoding.UTF8, "application/json")))
            .GetAwaiter().GetResult();
        Assert.That(initResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Act — list tools
        var listRequest = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/list",
            @params = new { }
        };
        var listJson = JsonSerializer.Serialize(listRequest);
        var listResponse = Task.Run(() =>
            client.PostAsync($"http://localhost:{port}/",
                new StringContent(listJson, System.Text.Encoding.UTF8, "application/json")))
            .GetAwaiter().GetResult();

        // Assert
        Assert.That(listResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var listBody = Task.Run(() => listResponse.Content.ReadAsStringAsync()).GetAwaiter().GetResult();
        Assert.That(listBody, Does.Contain("add_building"));
        Assert.That(listBody, Does.Contain("footprint coordinates"));
    }

    [Test]
    public void CallTool_AddBuilding_MissingArguments_ReturnsError()
    {
        // Arrange
        UrbanModel.Instance.Reset();
        _server.Start(0).GetAwaiter().GetResult();
        var port = _server.Port;

        // Initialize
        var initRequest = new
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

        using var client = new HttpClient();
        var initJson = JsonSerializer.Serialize(initRequest);
        var initResponse = Task.Run(() =>
            client.PostAsync($"http://localhost:{port}/",
                new StringContent(initJson, System.Text.Encoding.UTF8, "application/json")))
            .GetAwaiter().GetResult();
        Assert.That(initResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Act — call add_building with empty arguments
        var callRequest = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new
            {
                name = "add_building",
                arguments = new { }
            }
        };
        var callJson = JsonSerializer.Serialize(callRequest);
        var callResponse = Task.Run(() =>
            client.PostAsync($"http://localhost:{port}/",
                new StringContent(callJson, System.Text.Encoding.UTF8, "application/json")))
            .GetAwaiter().GetResult();

        // Assert — should receive error because items are required
        Assert.That(callResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = Task.Run(() => callResponse.Content.ReadAsStringAsync()).GetAwaiter().GetResult();
        Assert.That(body, Does.Contain("\"code\":-32602"));
        Assert.That(body, Does.Contain("requires at least one building"));
    }

}
