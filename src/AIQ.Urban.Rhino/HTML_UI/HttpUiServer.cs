using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Rhino;

namespace AIQ.Urban.RhinoInterface.HTML_UI;

public static class HttpUiServer
{
    private static readonly object Lock = new();
    private static HttpListener? _listener;
    private static bool _serverRunning;

    public static void Start(int port)
    {
        lock (Lock)
        {
            if (_serverRunning)
                return;

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();
            _serverRunning = true;
        }

        Task.Run(HandleRequestsAsync);
    }

    public static void Stop()
    {
        lock (Lock)
        {
            if (!_serverRunning)
                return;

            _serverRunning = false;
            _listener?.Stop();
            _listener?.Close();
            _listener = null;
        }
    }

    private static async Task HandleRequestsAsync()
    {
        while (_serverRunning && _listener is not null)
        {
            HttpListenerContext context;

            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (HttpListenerException)
            {
                return;
            }

            await HandleRequestAsync(context);
        }
    }

    private static async Task HandleRequestAsync(HttpListenerContext context)
    {
        var response = context.Response;

        try
        {
            if (context.Request.HttpMethod != "GET")
            {
                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                response.Close();
                return;
            }

            var filePath = ResolveStaticFilePath(context.Request.Url?.LocalPath ?? "/");

            if (!File.Exists(filePath))
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                await WriteResponseAsync(response, "404 Not Found", "text/plain");
                return;
            }

            var fileBytes = await File.ReadAllBytesAsync(filePath);
            response.ContentType = GetContentType(filePath);
            response.ContentLength64 = fileBytes.Length;
            await response.OutputStream.WriteAsync(fileBytes, 0, fileBytes.Length);
            response.Close();
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"AIQ Urban UI HTTP server error: {ex.Message}");

            if (response.OutputStream.CanWrite)
                response.Close();
        }
    }

    private static string ResolveStaticFilePath(string localPath)
    {
        var root = GetStaticRoot();
        var relativePath = localPath == "/" ? "index.html" : localPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            return Path.Combine(root, "index.html");

        return fullPath;
    }

    private static string GetStaticRoot()
    {
        var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            ?? AppContext.BaseDirectory;

        return Path.GetFullPath(Path.Combine(assemblyDirectory, "HTML_UI", "wwwroot"));
    }

    private static async Task WriteResponseAsync(HttpListenerResponse response, string content, string contentType)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        response.ContentType = contentType;
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        response.Close();
    }

    private static string GetContentType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".html" => "text/html",
            ".js" => "application/javascript",
            ".css" => "text/css",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".json" => "application/json",
            _ => "application/octet-stream",
        };
    }
}
