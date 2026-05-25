using AIQ.MCP.Models;
using AIQ.MCP.Services;
using AIQ.Urban.RhinoInterface;
using NUnit.Framework;
using System.Text.Json;
using McpServer = AIQ.MCP.Services.McpServer;

namespace AIQ.MCP.Tests.Services;

public sealed class FakePipelineExecutor : IPipelineExecutor
{
    public int ExecuteCallCount { get; private set; }
    public Action<PipelineConfigurations>? LastConfigure { get; private set; }

    public void Execute(Action<PipelineConfigurations> configure)
    {
        ExecuteCallCount++;
        LastConfigure = configure;
    }
}

public class PipelineExecutorTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Serializes any CallToolResult-like object to a JSON string for assertion.</summary>
    private static string SerializeResult(object result)
        => JsonSerializer.Serialize(result, JsonOptions);

    [Test]
    public void ExecuteAddBuilding_ValidRequest_ExecutesPipeline()
    {
        var executor = new FakePipelineExecutor();
        var server = new McpServer(executor);
        var request = new AddBuildingRequest([
            new BuildingItem([[0, 0], [10, 0], [10, 10], [0, 10], [0, 0]], 3.5, 5, "Residential")
        ]);

        var result = server.ExecuteAddBuilding(request);

        Assert.That(executor.ExecuteCallCount, Is.EqualTo(1));
        var json = SerializeResult(result);
        Assert.That(json, Does.Contain("elementGuids"));
    }

    [Test]
    public void ExecuteAddBuilding_ValidRequest_ReturnsElementGuids()
    {
        var executor = new FakePipelineExecutor();
        var server = new McpServer(executor);
        var request = new AddBuildingRequest([
            new BuildingItem([[0, 0], [10, 0], [10, 10], [0, 10], [0, 0]], 3.5, 5, "Residential"),
            new BuildingItem([[0, 0], [5, 0], [5, 5], [0, 5], [0, 0]], 4.0, 3, "Commercial"),
        ]);

        var result = server.ExecuteAddBuilding(request);

        var json = SerializeResult(result);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement.GetProperty("content");
        var text = content[0].GetProperty("text").GetString()!;
        using var inner = JsonDocument.Parse(text);
        var guids = inner.RootElement.GetProperty("elementGuids").EnumerateArray().ToList();
        Assert.That(guids, Has.Count.EqualTo(2));
    }

    [Test]
    public void ExecuteAddBuilding_MissingItems_Throws()
    {
        var executor = new FakePipelineExecutor();
        var server = new McpServer(executor);
        var request = new AddBuildingRequest([]);

        var ex = Assert.Catch(() =>
            server.ExecuteAddBuilding(request));
        Assert.That(ex!.Message, Does.Contain("requires at least one"));
        Assert.That(executor.ExecuteCallCount, Is.EqualTo(0));
    }

    [Test]
    public void ExecuteAddBuilding_NullRequest_Throws()
    {
        var executor = new FakePipelineExecutor();
        var server = new McpServer(executor);

        var ex = Assert.Catch(() =>
            server.ExecuteAddBuilding(null));
        Assert.That(ex!.Message, Does.Contain("requires at least one"));
        Assert.That(executor.ExecuteCallCount, Is.EqualTo(0));
    }

    [Test]
    public void ExecuteAddTransit_ValidRequest_ExecutesPipeline()
    {
        var executor = new FakePipelineExecutor();
        var server = new McpServer(executor);
        var request = new AddTransitRequest([
            new TransitItem([[0, 0], [10, 0]], 7.0, "Road")
        ]);

        var result = server.ExecuteAddTransit(request);

        Assert.That(executor.ExecuteCallCount, Is.EqualTo(1));
        var json = SerializeResult(result);
        Assert.That(json, Does.Contain("elementGuids"));
    }

    [Test]
    public void ExecuteAddTransit_ValidRequest_ReturnsCenterlineGuids()
    {
        var executor = new FakePipelineExecutor();
        var server = new McpServer(executor);
        var request = new AddTransitRequest([
            new TransitItem([[0, 0], [10, 0]], 7.0, "Road"),
            new TransitItem([[0, 5], [10, 5]], 3.0, "CyclePath"),
        ]);

        var result = server.ExecuteAddTransit(request);

        var json = SerializeResult(result);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement.GetProperty("content");
        var text = content[0].GetProperty("text").GetString()!;
        using var inner = JsonDocument.Parse(text);
        var guids = inner.RootElement.GetProperty("elementGuids").EnumerateArray().ToList();
        Assert.That(guids, Has.Count.EqualTo(2));
    }

    [Test]
    public void ExecuteAddTransit_MissingItems_Throws()
    {
        var executor = new FakePipelineExecutor();
        var server = new McpServer(executor);
        var request = new AddTransitRequest([]);

        var ex = Assert.Catch(() =>
            server.ExecuteAddTransit(request));
        Assert.That(ex!.Message, Does.Contain("requires at least one"));
        Assert.That(executor.ExecuteCallCount, Is.EqualTo(0));
    }

    [Test]
    public void ExecuteAddTransit_NullRequest_Throws()
    {
        var executor = new FakePipelineExecutor();
        var server = new McpServer(executor);

        var ex = Assert.Catch(() =>
            server.ExecuteAddTransit(null));
        Assert.That(ex!.Message, Does.Contain("requires at least one"));
        Assert.That(executor.ExecuteCallCount, Is.EqualTo(0));
    }

    [Test]
    public void ExecuteChangeBuildingUse_ValidRequest_ExecutesPipeline()
    {
        var executor = new FakePipelineExecutor();
        var server = new McpServer(executor);
        var request = new ChangeBuildingUseRequest([
            new ChangeUseItem(Guid.NewGuid(), "Commercial"),
        ]);

        var result = server.ExecuteChangeBuildingUse(request);

        Assert.That(executor.ExecuteCallCount, Is.EqualTo(1));
        var json = SerializeResult(result);
        Assert.That(json, Does.Contain("affectedGuids"));
    }

    [Test]
    public void ExecuteChangeBuildingUse_ValidRequest_ReturnsAffectedGuids()
    {
        var executor = new FakePipelineExecutor();
        var server = new McpServer(executor);
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var request = new ChangeBuildingUseRequest([
            new ChangeUseItem(id1, "Commercial"),
            new ChangeUseItem(id2, "Residential"),
        ]);

        var result = server.ExecuteChangeBuildingUse(request);

        var json = SerializeResult(result);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement.GetProperty("content");
        var text = content[0].GetProperty("text").GetString()!;
        using var inner = JsonDocument.Parse(text);
        var guids = inner.RootElement.GetProperty("affectedGuids").EnumerateArray()
            .Select(g => g.GetString()).ToList();
        Assert.That(guids, Has.Count.EqualTo(2));
        Assert.That(guids, Does.Contain(id1.ToString()));
        Assert.That(guids, Does.Contain(id2.ToString()));
    }

    [Test]
    public void ExecuteChangeBuildingUse_MultipleSameUse_ReturnsDistinctGuids()
    {
        var executor = new FakePipelineExecutor();
        var server = new McpServer(executor);
        var id = Guid.NewGuid();
        var request = new ChangeBuildingUseRequest([
            new ChangeUseItem(id, "Commercial"),
            new ChangeUseItem(id, "Commercial"),
        ]);

        var result = server.ExecuteChangeBuildingUse(request);

        var json = SerializeResult(result);
        using var doc = JsonDocument.Parse(json);
        var content = doc.RootElement.GetProperty("content");
        var text = content[0].GetProperty("text").GetString()!;
        using var inner = JsonDocument.Parse(text);
        var guids = inner.RootElement.GetProperty("affectedGuids").EnumerateArray()
            .Select(g => g.GetString()).ToList();
        Assert.That(guids, Has.Count.EqualTo(1));
        Assert.That(guids[0], Is.EqualTo(id.ToString()));
    }

    [Test]
    public void ExecuteChangeBuildingUse_MissingItems_Throws()
    {
        var executor = new FakePipelineExecutor();
        var server = new McpServer(executor);
        var request = new ChangeBuildingUseRequest([]);

        var ex = Assert.Catch(() =>
            server.ExecuteChangeBuildingUse(request));
        Assert.That(ex!.Message, Does.Contain("requires at least one"));
        Assert.That(executor.ExecuteCallCount, Is.EqualTo(0));
    }

    [Test]
    public void ExecuteChangeBuildingUse_NullRequest_Throws()
    {
        var executor = new FakePipelineExecutor();
        var server = new McpServer(executor);

        var ex = Assert.Catch(() =>
            server.ExecuteChangeBuildingUse(null));
        Assert.That(ex!.Message, Does.Contain("requires at least one"));
        Assert.That(executor.ExecuteCallCount, Is.EqualTo(0));
    }
}
