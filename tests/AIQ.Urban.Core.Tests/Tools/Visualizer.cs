using System;
using System.Diagnostics;
using System.IO;
using Rhino;
using Rhino.FileIO;

namespace AIQ.Urban.Core.Tests.Tools;

public static class Visualizer
{
    public static string View(RhinoDoc doc, string filePath = null)
    {
        if (doc.Objects.Count == 0)
            return string.Empty;

        var path = filePath ?? Path.Combine(Path.GetTempPath(), $"AIQUrban_{Guid.NewGuid():N}.glb");
        var options = new FileGltfWriteOptions
        {
            ExportLayers = true,
            MapZToY = true,
            ExportMaterials = true,
            UseDisplayColorForUnsetMaterials = true,
            CullBackfaces = false,
        };

        FileGltf.Write(path, doc, options);

#if DEBUG
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
#endif

        return path;
    }
}
