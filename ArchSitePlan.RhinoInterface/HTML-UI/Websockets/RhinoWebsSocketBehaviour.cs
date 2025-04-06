using ArchSitePlan.RhinoInterface.HTML_UI.HtmlCommsClasses;
using ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;
using Eto.Forms;
using Newtonsoft.Json;
using Rhino;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ArchSitePlan.RhinoInterface.HTML_UI.Websockets;
public class RhinoWebSocketBehavior : WebSocketBehavior
{
    protected override void OnMessage(MessageEventArgs e)
    {
        RhinoApp.WriteLine("Received message from Vue UI: " + e.Data);

        try
        {
            var messageObj = MessageConverter.ConvertMessageToJson(e.Data);
            IMessageToUI response = messageObj.HandleMessage();
            SendMessage(response);
        }
        catch (Exception ex)
        {
            RhinoApp.InvokeOnUiThread(() => MessageBox.Show(ex.Message));
            SendMessage(new EmptyMessage());
        }
    }

    public void SendMessage(IMessageToUI message)
    {
        string json = JsonConvert.SerializeObject(message);
        Send(JsonConvert.SerializeObject(message));
        RhinoApp.WriteLine("Sending message to UI\n" + json);
    }
}