using ArchSitePlan.UI.HTML_UI.HtmlCommsClasses;
using Newtonsoft.Json;
using Rhino;
using Rhino.Commands;
using System.Net;
using System.Text;
using System.Windows;

namespace ArchSitePlan.UI.HTML_UI;

public class ServerComms
{

    private static HttpListener _httpListener;
    private static bool _serverRunning = false;
    public static void StartServer()
    {
        StartHttpServer();
        RhinoApp.WriteLine("HTTP Server is running and listening for requests...");
    }

    private static void StartHttpServer()
    {
        _serverRunning = true;
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add("http://localhost:6300/");
        _httpListener.Start();
        Task.Run(() => HandleRequestsAsync());
    }

    private static async Task HandleRequestsAsync()
    {
        while (_serverRunning)
        {
            try
            {
                HttpListenerContext context = await _httpListener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                try
                {
                    // Handle CORS preflight request
                    if (request.HttpMethod == "OPTIONS")
                    {
                        response = AddCORSHeaders(response);
                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.Close();
                        continue; // Skip further processing for OPTIONS request
                    }

                    // Handle POST request (assuming Vue.js sends data via POST)
                    if (request.HttpMethod == "POST" ||
                    request.HttpMethod == "post")
                    {
                        using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                        {
                            string requestBody = await reader.ReadToEndAsync();
                            RhinoApp.WriteLine("Received data from Vue.js: " + requestBody);

                            // Process requestBody and use RhinoCommon to perform operations in Rhino
                            var messageObj = MessageConverter.ConvertMessageToJson(requestBody);
                            var responseMessage = messageObj.HandleMessage();
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

                            MessageBox.Show(buffer.Select(x => x.ToString()).ToList().ToString());
                            response.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    RhinoApp.InvokeOnUiThread(() =>
                    {
                        MessageBox.Show(ex.Message);
                    });

                    response.Close();
                }
            }
            // In case context and responses throw errors
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
           
        }
    }

    private static HttpListenerResponse AddCORSHeaders(HttpListenerResponse response)
    {
        response.AddHeader("Access-Control-Allow-Origin", "*");
        response.AddHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
        response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
        return response;
    }
}
