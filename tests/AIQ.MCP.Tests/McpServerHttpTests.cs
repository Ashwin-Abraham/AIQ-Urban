using AIQ.MCP.Services;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AIQ.MCP.Tests;

public class McpServerHttpTests
{
    [Test]
    public async Task TestHealth_ReturnsHealthyMessage()
    {
        await using var server = new McpServerHost();
        server.OnListTools((_, _) => ValueTask.FromResult(new ModelContextProtocol.Protocol.ListToolsResult { Tools = [] }));

        await server.Start(0);

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        // Step 1: Initialize session
        var initRequest = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:{server.Port}/mcp")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "initialize",
                    @params = new
                    {
                        protocolVersion = "2024-11-05",
                        capabilities = new { },
                        clientInfo = new { name = "test-client", version = "1.0" }
                    }
                }),
                Encoding.UTF8,
                "application/json")
        };
        initRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        initRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var initResponse = await client.SendAsync(initRequest);
        var initBody = await initResponse.Content.ReadAsStringAsync();

        // Extract session ID from response headers
        var sessionId = initResponse.Headers.Contains("Mcp-Session-Id")
            ? string.Join(",", initResponse.Headers.GetValues("Mcp-Session-Id"))
            : null;

        // Step 2: Call tools/list with session header
        var listRequest = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:{server.Port}/mcp")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    id = 2,
                    method = "tools/list"
                }),
                Encoding.UTF8,
                "application/json")
        };
        listRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        listRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (sessionId is not null)
            listRequest.Headers.Add("Mcp-Session-Id", sessionId);

        var listResponse = await client.SendAsync(listRequest);

        // Step 3: Call tools/call with test_health
        var callRequest = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:{server.Port}/mcp")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    id = 3,
                    method = "tools/call",
                    @params = new { name = "test_health", arguments = new { } }
                }),
                Encoding.UTF8,
                "application/json")
        };
        callRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        callRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (sessionId is not null)
            callRequest.Headers.Add("Mcp-Session-Id", sessionId);

        var callResponse = await client.SendAsync(callRequest);
        var callBody = await callResponse.Content.ReadAsStringAsync();

        Assert.That(callBody, Does.Contain("Your AIQ MCP Server is HEALTHY"),
            "Response should contain the health message. Got: " + callBody);

        await server.Stop();
    }

    [Test]
    public async Task ServerStartsAndMcpEndpointResponds()
    {
        await using var server = new McpServerHost();

        server.OnListTools(async (_, ct) =>
        {
            return new ModelContextProtocol.Protocol.ListToolsResult
            {
                Tools = []
            };
        });

        await server.Start(0);

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        var request = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:{server.Port}/mcp")
        {
            Content = new StringContent(
                """{"jsonrpc":"2.0","id":1,"method":"tools/list"}""",
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(body, Does.Contain("jsonrpc"),
            "Response should be a JSON-RPC message. Got: " + body);

        await server.Stop();
    }

    [Test]
    public async Task GetRoot_ReturnsOk()
    {
        await using var server = new McpServerHost();
        await server.Start(0);

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        var response = await client.GetAsync($"http://localhost:{server.Port}/");

        Assert.That(response, Is.Not.Null);

        await server.Stop();
    }
}
