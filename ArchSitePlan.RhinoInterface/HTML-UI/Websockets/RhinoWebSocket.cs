using ArchSitePlan.RhinoInterface.HTML_UI.HtmlCommsClasses;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Eto.Forms;
using Newtonsoft.Json;
using Rhino;
using WebSocketSharp;

namespace ArchSitePlan.RhinoInterface.HTML_UI.Websockets;

internal class RhinoWebSocket
{
    private WebSocket _ws;

    public RhinoWebSocket(string serverUrl)
    {
        _ws = new WebSocket(serverUrl);
        _ws.Connect();
        _ws.OnMessage += _ws_OnMessage;
    }

    private void _ws_OnMessage(object? sender, MessageEventArgs e)
    {
        try
        {
            var messageObj = MessageConverter.ConvertMessageToJson(e.Data);
            RhinoApp.WriteLine("Received message from UI\n" + e.Data);

            IMessageToUI response = messageObj.HandleMessage();
            SendMessage(response);
        }
        catch(Exception ex)
        {
            RhinoApp.InvokeOnUiThread(() => MessageBox.Show(ex.Message));
            SendMessage(new EmptyMessage());
        }
       
    }

    public void SendMessage(IMessageToUI message)
    {
        string json = JsonConvert.SerializeObject(message);
        RhinoApp.WriteLine("Sending message to UI\n" + json);
        _ws.Send(json);
    }

}
