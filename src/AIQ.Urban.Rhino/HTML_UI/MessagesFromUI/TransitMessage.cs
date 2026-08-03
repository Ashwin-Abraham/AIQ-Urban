using System;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Logging;
using AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using Microsoft.Extensions.Logging;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;

public class TransitMessage : IMessageFromUI<ModelStateMessage>
{
    public TypeOfMessage MessageType => TypeOfMessage.Transit;

    public double RoadWidth { get; init; }

    public ModelStateMessage HandleMessage()
    {
        var doc = RhinoDoc.ActiveDoc;
        var selectedIds = doc.Objects.GetSelectedObjects(false, false).Select(x => x.Id).ToList();

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var preparationStep = new AddTransitInputPreparationStep(selectedIds, RoadWidth, TransitType.Road);

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, preparationStep, loggerFactory)
            .WithTransitAdditionSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

        if (Urban_RhinoPlugin.Instance.EventHandler is { } handler)
            pipeline.WithEventHandler(handler);

        pipeline.Run();
        doc.Views.Redraw();

        return new ModelStateMessage(UrbanModel.Instance);
    }
}
