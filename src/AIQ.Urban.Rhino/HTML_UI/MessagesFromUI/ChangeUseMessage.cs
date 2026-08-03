using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Logging;
using AIQ.Urban.RhinoInterface.Pipeline.Outputters;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using Microsoft.Extensions.Logging;
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
            .Where(obj => obj.ObjectType is ObjectType.Brep or ObjectType.Curve)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more parcels, building envelopes, or floor plate curves first.");

        var use = UrbanModel.Instance.GetUse(Use);
        var geometryIds = selected.Select(obj => obj.Id).ToList();

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, loggerFactory)
            .WithDataGatherStep(new ElementUseChangeGatherStep(geometryIds, use))
            .WithUrbanElementStep(new ChangeParcelUseStep())
            .WithUrbanElementStep(new ChangeBuildingUseStep())
            .WithRhinoOutputter<ParcelOutputter>()
            .WithRhinoOutputter<BuildingOutputter>()
            .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

        if (Urban_RhinoPlugin.Instance?.EventHandler is { } handler)
            pipeline.WithEventHandler(handler);

        pipeline.Run();
        doc.Views.Redraw();

        return new ModelStateMessage(UrbanModel.Instance);
    }
}
