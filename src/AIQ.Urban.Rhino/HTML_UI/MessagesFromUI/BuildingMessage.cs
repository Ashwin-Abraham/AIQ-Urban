using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Logging;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
using Microsoft.Extensions.Logging;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.Urban.RhinoPlugin.HTML_UI.MessagesFromUI;

public class BuildingMessage : IMessageFromUI<ModelStateMessage>
{
    public TypeOfMessage MessageType => TypeOfMessage.Building;

    public int FloorCount { get; init; }

    public double FloorHeight { get; init; }

    public string manual_generationType { get; init; } = "Footprint";

    public ModelStateMessage HandleMessage()
    {
        var doc = RhinoDoc.ActiveDoc;
        var use = UrbanModel.Instance.GetUse(DefaultUses.UndefinedName);

        if (manual_generationType == "Mass")
            return HandleMassMode(doc, use);

        return HandleFootprintMode(doc, use);
    }

    private ModelStateMessage HandleFootprintMode(RhinoDoc doc, Use use)
    {
        var selected = doc.Objects.GetSelectedObjects(false, false)
            .Where(obj => obj.ObjectType == ObjectType.Curve)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more building footprint curves first.");

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var gatherStep = new SelectBuildingFootprintStep(selected, FloorHeight, FloorCount, use, loggerFactory);
        return RunPipeline(doc, gatherStep, loggerFactory);
    }

    private ModelStateMessage HandleMassMode(RhinoDoc doc, Use use)
    {
        var selected = doc.Objects.GetSelectedObjects(false, false)
            .Where(obj => obj.ObjectType is ObjectType.Brep or ObjectType.Extrusion)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more building envelope Breps or Extrusions first.");

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var gatherStep = new SelectBuildingEnvelopeStep(selected, FloorHeight, use, UrbanModel.Instance, loggerFactory);
        return RunPipeline(doc, gatherStep, loggerFactory);
    }

    private ModelStateMessage RunPipeline(RhinoDoc doc, IRhinoDataGatherStep gatherStep, ILoggerFactory loggerFactory)
    {
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, loggerFactory)
            .WithDataGatherStep(gatherStep)
            .WithBuildingAddOrUpdateSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

        if (Urban_RhinoPlugin.Instance.EventHandler is { } handler)
            pipeline.WithEventHandler(handler);

        pipeline.Run();
        doc.Views.Redraw();

        return new ModelStateMessage(UrbanModel.Instance);
    }
}
