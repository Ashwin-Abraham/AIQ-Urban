using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using ArchSitePlan.RhinoInterface.Actions;
using ArchSitePlan.RhinoInterface.HTML_UI.HtmlCommsClasses;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Eto.Forms;
using Newtonsoft.Json;
using Rhino;
using MessageBox = Eto.Forms.MessageBox;

namespace ArchSitePlan.RhinoInterface.HTML_UI;

public class ServerComms
{

    private static HttpListener _httpListener;
    private static bool _serverRunning = false;
    private static bool _htmlServed = false;
    public static void StartServer()
    {
        StartHttpServer();
        RhinoApp.WriteLine("HTTP Server is running and listening for requests...");
    }

    private static void StartHttpServer()
    {
        if (_serverRunning) return;
        _serverRunning = true;
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add("http://localhost:6300/");
        _httpListener.Start();
        //SetupVueIndexPage();

        Task.Run(() => HandleRequestsAsync());


        // Create a new form (window)
        //var htmlWindow = new HtmlForm();
        //try
        //{
        //    RhinoApp.InvokeOnUiThread(() =>
        //    {
        //        htmlWindow.Show();
        //    });

        //}
        //catch (Exception ex)
        //{
        //    MessageBox.Show(ex.Message);
        //}


    }

    private static void SetupVueIndexPage()
    {
        try
        {
            HttpListenerContext context = _httpListener.GetContext();
            HttpListenerResponse response = context.Response;

            string distPath = @"C:\Users\AshwinAbraham\source\repos\HelloVueJS\HelloVue\dist";
            // Get the requested file path, defaulting to index.html if it's the root
            string relativePath = context.Request.Url.LocalPath == "/" ? "index.html" : context.Request.Url.LocalPath.TrimStart('/');
            string filePath = Path.Combine(distPath, relativePath);

            if (File.Exists(filePath))
            {
                var fileBytes = File.ReadAllBytes(filePath);
                response.ContentType = GetContentType(filePath);
                response.ContentLength64 = fileBytes.Length;
                response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
            }
            else
            {
                response.StatusCode = 404;
                byte[] errorBytes = Encoding.UTF8.GetBytes("404 Not Found");
                response.ContentLength64 = errorBytes.Length;
                response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
            }
        }
        catch(Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
       
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
                string distPath = @"C:\Users\AshwinAbraham\source\repos\aiqurban-ui\dist";
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
                        var vm = RhinoEventHandler.HTMLViewModel;
                        string requestBody = await reader.ReadToEndAsync();
                        RhinoApp.WriteLine("Received data from Vue.js: " + requestBody);

                        // Process requestBody and use RhinoCommon to perform operations in Rhino
                        var messageObj = MessageConverter.ConvertMessageToJson(requestBody);

                        IMessageToUI responseMessage = null;
                        //RhinoApp.InvokeOnUiThread(() =>
                        //responseMessage = messageObj.HandleMessage());

                        responseMessage = messageObj.HandleMessage();

                        response = AddCORSHeaders(response);

                        // Send response back to Vue.js
                        //byte[] buffer = Encoding.UTF8.GetBytes("{ \"status\": \"success\" }");
                        string json = JsonConvert.SerializeObject(responseMessage);
                        byte[] buffer = Encoding.UTF8.GetBytes(json);
                        response.ContentLength64 = buffer.Length;
                        response.ContentType = "application/json";

                        using (var output = response.OutputStream)
                        {
                            await output.WriteAsync(buffer, 0, buffer.Length);
                        }

                        //MessageBox.Show(buffer.Select(x => x.ToString()).ToList().ToString());
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
