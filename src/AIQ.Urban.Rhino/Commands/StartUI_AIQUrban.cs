using System;
using AIQ.Urban.Core;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Logging;
using AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;
using AIQ.Urban.RhinoPlugin.HTML_UI;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using Microsoft.Extensions.Logging;
using Rhino;
using Rhino.Commands;
using MessageBox = Eto.Forms.MessageBox;

namespace AIQ.Urban.RhinoPlugin;

public class StartUI_AIQUrban : Command
{
    private const int HttpServerPort = 6300;
    private const int WebSocketServerPort = 6400;

    public StartUI_AIQUrban()
    {
        Instance = this;
    }

    public static StartUI_AIQUrban Instance { get; private set; }

    public override string EnglishName => "StartUI_AIQUrban";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        try
        {
            var preparationStep = new LoadDocumentInputPreparationStep();
            var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, preparationStep, loggerFactory)
                .WithDocumentLoadSteps();
            pipeline.Run();

            WebSocketUiServer.CacheModelState(new ModelStateMessage(UrbanModel.Instance));
            Urban_RhinoPlugin.Instance.PublishCurrentSelection();
            HttpUiServer.Start(HttpServerPort);
            WebSocketUiServer.Start(WebSocketServerPort);

            Urban_RhinoPlugin.Instance.StartMcpProcess();

            UrbanHtmlUi.OpenWindow(HttpServerPort, StopServers);

            RhinoApp.WriteLine("AIQ Urban UI started at http://localhost:{0}/", HttpServerPort);
            return Result.Success;
        }
        catch (Exception ex)
        {
            StopServers();
            MessageBox.Show(ex.Message, "Failed to start AIQ Urban UI");
            return Result.Failure;
        }
    }

    private static void StopServers()
    {
        HttpUiServer.Stop();
        WebSocketUiServer.Stop();
    }
}
