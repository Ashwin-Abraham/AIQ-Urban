using AIQ.MCP.Services;
using AIQ.Urban.Core;
using NUnit.Framework;
using Rhino;
using Rhino.Testing.Fixtures;
using System.Threading.Tasks;

namespace AIQ.MCP.Tests;

[RhinoTestFixture]
public class McpServerStartupTests
{
    [SetUp]
    public void SetUp()
    {
        UrbanModel.Instance.Reset();
    }

    [Test]
    public async Task Start_BindsToPort()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        await using var server = new McpServer(doc, UrbanModel.Instance);

        // Act
        await server.Start(0);

        // Assert
        Assert.That(server.Port, Is.GreaterThan(0));

        // Cleanup
        await server.Stop();
    }

    [Test]
    public async Task StartStop_LifecycleSucceeds()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        await using var server = new McpServer(doc, UrbanModel.Instance);

        // Act & Assert — no exceptions
        Assert.DoesNotThrowAsync(async () =>
        {
            await server.Start(0);
            Assert.That(server.Port, Is.GreaterThan(0));
            await server.Stop();
        });
    }

    [Test]
    public async Task Stop_BeforeStart_DoesNotThrow()
    {
        // Arrange
        using var doc = RhinoDoc.CreateHeadless(null);
        await using var server = new McpServer(doc, UrbanModel.Instance);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await server.Stop());
    }
}
