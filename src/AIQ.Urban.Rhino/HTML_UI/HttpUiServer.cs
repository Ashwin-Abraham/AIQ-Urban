using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;
using Rhino;

namespace AIQ.Urban.RhinoPlugin.HTML_UI;

public static class HttpUiServer
{
    private static readonly object Lock = new();
    private static HttpListener? _listener;
    private static bool _serverRunning;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.HttpMethod == "OPTIONS")
            {
                AddCorsHeaders(response);
                response.StatusCode = (int)HttpStatusCode.OK;
                response.Close();
                return;
            }

            if (request.HttpMethod == "POST")
            {
                await HandlePostAsync(request, response);
                return;
            }

            if (request.HttpMethod != "GET")
            {
                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                response.Close();
                return;
            }

            await HandleGetAsync(request, response);
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"AIQ Urban UI HTTP server error: {ex.Message}");

            if (response.OutputStream.CanWrite)
            {
                try { response.Close(); }
                catch { /* ignore */ }
            }
        }
    }

    private static async Task HandlePostAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var body = await reader.ReadToEndAsync();

        IMessageFromUI<IMessageToUI> messageObj;

        try
        {
            messageObj = MessageConverter.Convert(body);
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"AIQ Urban UI: Invalid message: {ex.Message}");
            AddCorsHeaders(response);
            await WriteJsonErrorAsync(response, $"Invalid message: {ex.Message}");
            return;
        }

        IMessageToUI? result = null;
        var tcs = new TaskCompletionSource<bool>();

        RhinoApp.InvokeOnUiThread(() =>
        {
            try
            {
                result = messageObj.HandleMessage();
                RhinoApp.WriteLine(result.ToString());
                RhinoApp.WriteLine(result.Type);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"AIQ Urban UI: Handler error: {ex.Message}");
                result = new ErrorMessage(ex.Message);
            }
            finally
            {
                tcs.SetResult(true);
            }
        });

        await tcs.Task;
        AddCorsHeaders(response);
        await WriteJsonResponseAsync(response, result!);
    }

    private static async Task HandleGetAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        var filePath = ResolveStaticFilePath(request.Url?.LocalPath ?? "/");

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

    private static void AddCorsHeaders(HttpListenerResponse response)
    {
        response.AddHeader("Access-Control-Allow-Origin", "*");
        response.AddHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
        response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
    }

    private static async Task WriteJsonResponseAsync(HttpListenerResponse response, IMessageToUI message)
    {
        var json = JsonSerializer.Serialize((object)message, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        response.Close();
    }

    private static async Task WriteJsonErrorAsync(HttpListenerResponse response, string error)
    {
        var json = JsonSerializer.Serialize(new { Type = "Error", Message = error }, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        response.Close();
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
