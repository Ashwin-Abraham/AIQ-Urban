using System;
using System.IO;

namespace AIQ.Urban.RhinoPlugin.HTML_UI;

internal sealed class InstallationIdProvider
{
    private const string InstallationIdFileName = "installation-id";

    internal static string DefaultDirectoryPath
    {
        get
        {
            var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localApplicationData))
                throw new InvalidOperationException("The local application-data directory is unavailable.");

            return Path.Combine(localApplicationData, "AIQ Urban");
        }
    }

    internal string FilePath { get; }

    internal InstallationIdProvider(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        FilePath = Path.Combine(directoryPath, InstallationIdFileName);
    }

    internal string GetOrCreate(out string warning)
    {
        warning = string.Empty;
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        if (File.Exists(FilePath))
        {
            var storedValue = File.ReadAllText(FilePath).Trim();
            if (TryNormalizeVersion4Id(storedValue, out var installationId))
                return installationId;

            warning = "The persisted Installation ID was invalid and has been replaced.";
        }

        var newInstallationId = Guid.NewGuid().ToString("D");
        File.WriteAllText(FilePath, newInstallationId);
        return newInstallationId;
    }

    private static bool TryNormalizeVersion4Id(string value, out string installationId)
    {
        installationId = string.Empty;

        if (!Guid.TryParseExact(value, "D", out var parsedId))
            return false;

        var canonicalId = parsedId.ToString("D");
        if (canonicalId[14] != '4' || !"89ab".Contains(canonicalId[19]))
            return false;

        installationId = canonicalId;
        return true;
    }
}
