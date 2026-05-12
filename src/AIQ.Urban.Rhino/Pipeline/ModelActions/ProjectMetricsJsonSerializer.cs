using System.IO;
using System.Text.Json;
using AIQ.Urban.Core.Models.Metrics;
using AIQ.Urban.RhinoPlugin.Utils;

namespace AIQ.Urban.RhinoPlugin.Pipeline.ModelActions;

/// <summary>Serializes <see cref="ProjectMetrics"/> to a JSON file in the user's Documents folder.</summary>
internal static class ProjectMetricsJsonSerializer
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>Write <paramref name="metrics"/> to the JSON file at <paramref name="filePath"/>.</summary>
    internal static void Save(ProjectMetrics metrics, string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(metrics, _jsonOptions);
        File.WriteAllText(filePath, json);
    }
}
