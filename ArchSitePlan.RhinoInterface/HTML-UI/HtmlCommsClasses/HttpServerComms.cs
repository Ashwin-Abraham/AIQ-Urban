using ArchSitePlan.RhinoInterface.Constants;
using ArchSitePlan.RhinoInterface.CoreModelInterface;
using ArchSitePlan.RhinoInterface.HTML_UI.HtmlCommsClasses;
using Newtonsoft.Json;
using Rhino;
using System.Net;
using System.Text;
using MessageBox = Eto.Forms.MessageBox;

namespace ArchSitePlan.RhinoInterface.HTML_UI;

public class HttpServerComms
{

    private static HttpListener _httpListener = new();
    private static bool _serverRunning = false;

    public static void StartServer(int port)
    {
        if (_serverRunning) return;
        _serverRunning = true;
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add($"http://localhost:{port}/");
        _httpListener.Start();

        Task.Run(() => HandleRequestsAsync());
    }

    public static void StopServer()
    {
        if (!_serverRunning) return;
        _serverRunning = false;
        _httpListener.Stop();
        _httpListener.Close();
    }

    private static async Task HandleRequestsAsync()
    {
        while (_serverRunning)
        {
            RhinoApp.InvokeOnUiThread(() =>
            {
                RhinoDoc.ActiveDoc.Views.EnableRedraw(true, true, true);
                RhinoDoc.ActiveDoc.Views.Redraw();
            });

            HttpListenerContext context = await _httpListener.GetContextAsync();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                string distPath = FilePathConstants.VueHtmlPath;
                // Get the requested file path, defaulting to index.html if it's the root
                string relativePath = context.Request.Url.LocalPath == "/" ? "index.html" : context.Request.Url.LocalPath.TrimStart('/');
                string filePath = Path.Combine(distPath, relativePath);

                // Handle CORS preflight request
                if (request.HttpMethod == "OPTIONS")
                {
                    response = AddCORSHeaders(response);
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.Close();
                    continue;
                    // Skip further processing for OPTIONS request
                }

                // Handle POST request (assuming Vue.js sends data via POST)
                if (request.HttpMethod == "POST" ||
                request.HttpMethod == "post")
                {
                    using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string requestBody = await reader.ReadToEndAsync();

                        // Process requestBody and perform operations in Rhino
                        var messageObj = MessageConverter.ConvertMessageToJson(requestBody);
                        RhinoEventHandler.UseStatsMessage.Increment(messageObj.MessageType.ToString());

                        var responseMessage = messageObj.HandleMessage();

                        response = AddCORSHeaders(response);

                        // Send response back to Vue.js
                        string json = JsonConvert.SerializeObject(responseMessage);
                        byte[] buffer = Encoding.UTF8.GetBytes(json);
                        response.ContentLength64 = buffer.Length;
                        response.ContentType = "application/json";

                        using (var output = response.OutputStream)
                        {
                            await output.WriteAsync(buffer, 0, buffer.Length);
                        }

                        response.Close();
                    }
                }
                
                if (request.HttpMethod != "GET") continue;
                
                if (File.Exists(filePath))
                {
                    var fileBytes = File.ReadAllBytes(filePath);
                    response.ContentType = GetContentType(filePath);
                    response.ContentLength64 = fileBytes.Length;
                    await response.OutputStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                }
                else
                {
                    response.StatusCode = 404;
                    byte[] errorBytes = Encoding.UTF8.GetBytes("404 Not Found");
                    response.ContentLength64 = errorBytes.Length;
                    await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                }
            }
            catch (Exception ex)
            {
                RhinoApp.InvokeOnUiThread(() =>
                {
                    MessageBox.Show(ex.Message);
                });

                if (response.OutputStream.CanWrite)
                {
                    response.Close();
                }
            }

            
        }
    }

    private static string GetContentType(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLower();
        return ext switch
        {
            ".html" => "text/html",
            ".js" => "application/javascript",
            ".css" => "text/css",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "application/octet-stream",
        };
    }

    private static HttpListenerResponse AddCORSHeaders(HttpListenerResponse response)
    {
        response.AddHeader("Access-Control-Allow-Origin", "*");
        response.AddHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
        response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
        return response;
    }
}
