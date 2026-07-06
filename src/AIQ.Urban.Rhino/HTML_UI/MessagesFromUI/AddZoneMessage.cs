using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;

public class AddZoneMessage : IMessageFromUI<ModelStateMessage>
{
    public TypeOfMessage MessageType => TypeOfMessage.Zone;

    public string Name { get; init; } = string.Empty;

    public ModelStateMessage HandleMessage()
    {
        var doc = RhinoDoc.ActiveDoc ?? throw new Exception("No active Rhino document.");
        var selected = doc.Objects.GetSelectedObjects(false, false)
            .Where(obj => obj.ObjectType == ObjectType.Curve)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one zone boundary curve first.");

        if (selected.Count > 1)
            throw new Exception("Select exactly one zone boundary curve first.");

        var gatherStep = new SelectZoneBoundaryStep(selected, Name);
        var creationData = gatherStep.Run(doc);
        var idsToDelete = creationData.OfType<ZoneData>().Select(zone => zone.Boundary.Id).ToList();

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(gatherStep)
            .WithZoneAdditionSteps([], idsToDelete)
            .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

        if (Urban_RhinoPlugin.Instance.EventHandler is { } handler)
            pipeline.WithEventHandler(handler);

        pipeline.Run();
        doc.Views.Redraw();

        return new ModelStateMessage(UrbanModel.Instance);
    }
}
