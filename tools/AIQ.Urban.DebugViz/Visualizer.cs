using System.Diagnostics;
using System.Net;
using Rhino;
using Rhino.FileIO;

namespace AIQ.Urban.DebugViz;

public static class Visualizer
{
    public static string View(RhinoDoc doc, string filePath = null, bool viewLocal = true)
    {
        return viewLocal
            ? ViewLocal(doc, filePath)
            : ViewBabylon(doc, filePath);
    }

    public static string ViewLocal(RhinoDoc doc, string filePath = null)
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

    public static string ViewBabylon(RhinoDoc doc, string filePath = null)
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

        var dir = Path.GetDirectoryName(path)!;
        var port = FindAvailablePort(8080);

        Task.Run(() => RunServer(dir, port));

        var glbUrl = $"http://localhost:{port}/{Path.GetFileName(path)}";
        var sandboxUrl = $"https://sandbox.babylonjs.com/?file={Uri.EscapeDataString(glbUrl)}";

#if DEBUG
        Process.Start(new ProcessStartInfo(sandboxUrl) { UseShellExecute = true });
#endif

        return sandboxUrl;
    }

    private static int FindAvailablePort(int startPort)
    {
        for (var port = startPort; port < startPort + 100; port++)
        {
            try
            {
                using var listener = new HttpListener();
                listener.Prefixes.Add($"http://localhost:{port}/");
                listener.Start();
                listener.Stop();
                return port;
            }
            catch (HttpListenerException)
            {
                continue;
            }
        }

        return startPort;
    }

    private static void RunServer(string directory, int port)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        try
        {
            while (true)
            {
                var context = listener.GetContext();
                var response = context.Response;

                response.Headers.Add("Access-Control-Allow-Origin", "*");

                var relativePath = context.Request.Url!.AbsolutePath.TrimStart('/');
                if (string.IsNullOrEmpty(relativePath))
                {
                    response.StatusCode = 404;
                    response.Close();
                    continue;
                }

                var filePath = Path.Combine(directory, relativePath);

                if (!File.Exists(filePath))
                {
                    response.StatusCode = 404;
                    response.Close();
                    continue;
                }

                var content = File.ReadAllBytes(filePath);
                response.ContentType = GetMimeType(filePath);
                response.ContentLength64 = content.Length;
                response.OutputStream.Write(content, 0, content.Length);
                response.Close();
            }
        }
        catch (HttpListenerException)
        {
        }
    }

    private static string GetMimeType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".glb" => "model/gltf-binary",
            ".gltf" => "model/gltf+json",
            ".bin" => "application/octet-stream",
            ".js" => "application/javascript",
            ".css" => "text/css",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream",
        };
    }
}
