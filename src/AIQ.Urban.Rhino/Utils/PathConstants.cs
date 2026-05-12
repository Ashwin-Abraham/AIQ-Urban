using System;
using System.IO;
using Rhino;

namespace AIQ.Urban.RhinoPlugin.Utils;

/// <summary>Well-known file paths used across the plugin.</summary>
internal static class PathConstants
{
    private const string _rootFolderName = "AIQ Urban";

    /// <summary>Root folder under %USERPROFILE%\Documents for AIQ Urban output files.</summary>
    public static string RootOutputFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        _rootFolderName);

    /// <summary>Full path to the project-metrics.json file for the given Rhino document.</summary>
    public static string ProjectMetricsFilePath(RhinoDoc doc)
    {
        var docName = string.IsNullOrEmpty(doc.Name)
            ? "Untitled"
            : Path.GetFileNameWithoutExtension(doc.Name);
        return Path.Combine(RootOutputFolder, $"{docName}-project-metrics.json");
    }
}
