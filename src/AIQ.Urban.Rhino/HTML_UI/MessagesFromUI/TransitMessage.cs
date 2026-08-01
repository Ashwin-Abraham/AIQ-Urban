using System;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Logging;
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
        var selected = doc.Objects.GetSelectedObjects(false, false)
            .Where(obj => obj.ObjectType == ObjectType.Curve)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more transit centerline curves first.");

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var gatherStep = new SelectTransitCenterlinesStep(selected, RoadWidth, TransitType.Road, loggerFactory);

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, loggerFactory)
            .WithDataGatherStep(gatherStep)
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
