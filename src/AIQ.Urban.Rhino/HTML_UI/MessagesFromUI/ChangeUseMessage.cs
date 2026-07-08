using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;

public class ChangeUseMessage : IMessageFromUI<ModelStateMessage>
{
    public TypeOfMessage MessageType => TypeOfMessage.Landuse;

    public string Use { get; init; } = string.Empty;

    public ModelStateMessage HandleMessage()
    {
        var doc = RhinoDoc.ActiveDoc ?? throw new Exception("No active Rhino document.");
        var selected = doc.Objects.GetSelectedObjects(false, false)
            .Where(obj => obj.ObjectType == ObjectType.Brep)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more parcel outline curves first.");

        var use = DefaultUses.Resolve(Use);
        var parcelGeometryIds = selected.Select(obj => obj.Id).ToList();
        var changeStep = new ChangeParcelUseStep(parcelGeometryIds, use);

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithUrbanElementStep(changeStep)
            .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

        if (Urban_RhinoPlugin.Instance.EventHandler is { } handler)
            pipeline.WithEventHandler(handler);

        pipeline.Run();
        doc.Views.Redraw();

        return new ModelStateMessage(UrbanModel.Instance);
    }
}
