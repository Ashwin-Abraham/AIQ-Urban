using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

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

        List<TransitData> transitData = [];
        List<Guid> centerlineIds = [];

        foreach (var obj in selected)
        {
            if (obj.Geometry is not Curve curve)
                continue;

            var curve2D = new Curve2D(curve, obj.Id);
            transitData.Add(new TransitData(curve2D, RoadWidth, TransitType.Road));
            centerlineIds.Add(obj.Id);
        }

        if (transitData.Count == 0)
            throw new Exception("Selected objects must be curves.");

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithTransitAdditionSteps(transitData, centerlineIds)
            .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

        if (Urban_RhinoPlugin.Instance.EventHandler is { } handler)
            pipeline.WithEventHandler(handler);

        pipeline.Run();
        doc.Views.Redraw();

        return new ModelStateMessage(UrbanModel.Instance);
    }
}
