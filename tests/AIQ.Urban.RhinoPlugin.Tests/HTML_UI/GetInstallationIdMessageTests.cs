using System;
using System.IO;
using System.Text.Json;
using AIQ.Urban.RhinoPlugin.HTML_UI;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;
using NUnit.Framework;

namespace AIQ.Urban.RhinoInterface.Tests;

public class GetInstallationIdMessageTests
{
    private string _tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "AIQ.Urban.GetInstallationIdMessageTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, recursive: true);
    }

    [Test]
    public void HandleMessage_ValidProvider_ReturnsInstallationIdMessage()
    {
        // Arrange
        const string storedId = "e01ca6d1-921f-4ae7-83e6-2e608f946846";
        var provider = new InstallationIdProvider(_tempDirectory);
        File.WriteAllText(provider.FilePath, storedId);
        var message = new GetInstallationIdMessage(provider);

        // Act
        var response = message.HandleMessage();

        // Preview
        TestContext.WriteLine($"{response.Type}: {response.InstallationId}");

        // Assert
        Assert.That(response.Type, Is.EqualTo("InstallationIdMessage"));
        Assert.That(response.InstallationId, Is.EqualTo(storedId));
    }

    [Test]
    public void HandleMessage_SerializedResponse_UsesCamelCaseContract()
    {
        // Arrange
        const string storedId = "e01ca6d1-921f-4ae7-83e6-2e608f946846";
        var provider = new InstallationIdProvider(_tempDirectory);
        File.WriteAllText(provider.FilePath, storedId);
        var message = new GetInstallationIdMessage(provider);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        var json = JsonSerializer.Serialize((object)message.HandleMessage(), options);
        using var document = JsonDocument.Parse(json);

        // Preview
        TestContext.WriteLine($"Serialized response: {json}");

        // Assert
        Assert.That(document.RootElement.GetProperty("type").GetString(), Is.EqualTo("InstallationIdMessage"));
        Assert.That(document.RootElement.GetProperty("installationId").GetString(), Is.EqualTo(storedId));
    }

    [Test]
    public void HandleMessage_ProviderFails_PropagatesException()
    {
        // Arrange
        var fileInsteadOfDirectory = Path.Combine(_tempDirectory, "not-a-directory");
        File.WriteAllText(fileInsteadOfDirectory, "content");
        var provider = new InstallationIdProvider(fileInsteadOfDirectory);
        var message = new GetInstallationIdMessage(provider);

        // Act
        TestDelegate handleMessage = () => message.HandleMessage();

        // Preview
        TestContext.WriteLine($"Invalid provider directory: {fileInsteadOfDirectory}");

        // Assert
        Assert.That(handleMessage, Throws.Exception);
    }
}
