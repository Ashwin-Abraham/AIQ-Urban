using System;
using System.Drawing;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Logging;
using AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using Microsoft.Extensions.Logging;
using Rhino;

namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;

public class LanduseListEditMessage : IMessageFromUI<ModelStateMessage>
{
    public TypeOfMessage MessageType => TypeOfMessage.LanduseListEdit;

    public string useEditType { get; init; } = string.Empty;
    public string name { get; init; } = string.Empty;
    public string color { get; init; } = string.Empty;
    public string id { get; init; } = string.Empty;

    public ModelStateMessage HandleMessage()
    {
        var doc = RhinoDoc.ActiveDoc ?? throw new Exception("No active Rhino document.");
        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var preparationStep = new ModifyUseListInputPreparationStep(useEditType, name, color, id);
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, preparationStep, loggerFactory)
            .WithLanduseListEditSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

        if (Urban_RhinoPlugin.Instance.EventHandler is { } handler)
            pipeline.WithEventHandler(handler);

        pipeline.Run();
        doc.Views.Redraw();

        return new ModelStateMessage(UrbanModel.Instance);
    }

    internal static System.Drawing.Color ParseColor(string colorString)
    {
        return ModifyUseListInputPreparationStep.ParseColor(colorString);
    }
}
