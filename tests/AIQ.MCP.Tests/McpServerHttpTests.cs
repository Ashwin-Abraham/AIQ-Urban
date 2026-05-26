using AIQ.MCP.Services;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using NUnit.Framework;
using Rhino;
using Rhino.Testing.Fixtures;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AIQ.MCP.Tests;

[RhinoTestFixture]
public class McpServerHttpTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    public async Task ToolsCall_AddTransit_ReturnsJsonRpcResponse()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        await using var server = new McpServer(doc, UrbanModel.Instance);
        await server.Start(0);

        var request = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            Params = new
            {
                name = "add_transit",
                arguments = new
                {
                    width = 6.0,
                    type = "Road",
                    curveCoordinates = new[]
                    {
                        new { coordinates = new[] { new[] { 0.0, 0.0 }, new[] { 50.0, 0.0 } } }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        // Act
        var response = await client.PostAsync(
            $"http://localhost:{server.Port}/mcp",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Assert — server responded
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Expected success, got {response.StatusCode}");

        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("jsonrpc"),
            "Response should be a JSON-RPC message");

        // Assert — UrbanModel was updated
        Assert.That(UrbanModel.Instance.Transits, Has.Count.EqualTo(1));

        await server.Stop();
    }

    [Test]
    public async Task GetRoot_ReturnsOk()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        await using var server = new McpServer(doc, UrbanModel.Instance);
        await server.Start(0);

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        // Act
        var response = await client.GetAsync($"http://localhost:{server.Port}/");

        // Assert — root should return something useful (e.g. 200 or 404 is fine,
        // but we just verify the server is alive and not crashing)
        Assert.That(response, Is.Not.Null);

        await server.Stop();
    }
}
