using System;
using System.IO;
using AIQ.Urban.RhinoPlugin.HTML_UI;
using NUnit.Framework;

namespace AIQ.Urban.RhinoInterface.Tests;

public class InstallationIdProviderTests
{
    private string _tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "AIQ.Urban.InstallationIdProviderTests",
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
    public void GetOrCreate_MissingFile_CreatesCanonicalVersion4Id()
    {
        // Arrange
        var provider = new InstallationIdProvider(_tempDirectory);

        // Act
        var installationId = provider.GetOrCreate(out var warning);

        // Preview
        TestContext.WriteLine($"Generated Installation ID: {installationId}");

        // Assert
        Assert.That(warning, Is.EqualTo(string.Empty));
        Assert.That(
            installationId,
            Does.Match("^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$"));
        Assert.That(File.ReadAllText(provider.FilePath), Is.EqualTo(installationId));
    }

    [Test]
    public void GetOrCreate_ExistingValidFile_ReturnsStoredId()
    {
        // Arrange
        const string storedId = "2e0c1456-c8c7-44d6-a938-9ca67a83006f";
        var provider = new InstallationIdProvider(_tempDirectory);
        File.WriteAllText(provider.FilePath, storedId);

        // Act
        var installationId = provider.GetOrCreate(out var warning);

        // Preview
        TestContext.WriteLine($"Retrieved Installation ID: {installationId}");

        // Assert
        Assert.That(warning, Is.EqualTo(string.Empty));
        Assert.That(installationId, Is.EqualTo(storedId));
    }

    [TestCase("MalformedText")]
    [TestCase("VersionOneUuid")]
    public void GetOrCreate_InvalidFile_ReplacesIdAndLogsWarning(string caseName)
    {
        // Arrange
        var testData = GetInvalidIdTestData(caseName);
        var provider = new InstallationIdProvider(_tempDirectory);
        File.WriteAllText(provider.FilePath, testData.StoredValue);

        // Act
        var installationId = provider.GetOrCreate(out var warning);

        // Preview
        TestContext.WriteLine(
            $"{testData.CaseName}: replaced '{testData.StoredValue}' with '{installationId}'. Warning: {warning}");

        // Assert
        Assert.That(installationId, Is.Not.EqualTo(testData.StoredValue));
        Assert.That(File.ReadAllText(provider.FilePath), Is.EqualTo(installationId));
        Assert.That(warning, Is.Not.EqualTo(string.Empty));
    }

    [Test]
    public void GetOrCreate_FileChangesBetweenRequests_ReturnsCurrentStoredId()
    {
        // Arrange
        const string firstId = "e415ace0-da36-4616-81d0-299b9af6d638";
        const string secondId = "a3570d61-b49d-44f7-bd00-a4f7283e6e0b";
        var provider = new InstallationIdProvider(_tempDirectory);
        File.WriteAllText(provider.FilePath, firstId);
        provider.GetOrCreate(out _);
        File.WriteAllText(provider.FilePath, secondId);

        // Act
        var installationId = provider.GetOrCreate(out var warning);

        // Preview
        TestContext.WriteLine($"Installation ID after external file change: {installationId}");

        // Assert
        Assert.That(warning, Is.EqualTo(string.Empty));
        Assert.That(installationId, Is.EqualTo(secondId));
    }

    [Test]
    public void GetOrCreate_PersistenceFails_Throws()
    {
        // Arrange
        var fileInsteadOfDirectory = Path.Combine(_tempDirectory, "not-a-directory");
        File.WriteAllText(fileInsteadOfDirectory, "content");
        var provider = new InstallationIdProvider(fileInsteadOfDirectory);

        // Act
        TestDelegate getOrCreate = () => provider.GetOrCreate(out _);

        // Preview
        TestContext.WriteLine($"Invalid persistence directory: {fileInsteadOfDirectory}");

        // Assert
        Assert.That(getOrCreate, Throws.Exception);
    }

    private static InvalidInstallationIdTestCase GetInvalidIdTestData(string caseName)
    {
        return caseName switch
        {
            "MalformedText" => new InvalidInstallationIdTestCase(
                CaseName: "MalformedText",
                StoredValue: "not-an-installation-id"),
            "VersionOneUuid" => new InvalidInstallationIdTestCase(
                CaseName: "VersionOneUuid",
                StoredValue: "6ba7b810-9dad-11d1-80b4-00c04fd430c8"),
            _ => throw new ArgumentException($"Unknown case: {caseName}")
        };
    }

    private sealed record InvalidInstallationIdTestCase(
        string CaseName,
        string StoredValue);
}
