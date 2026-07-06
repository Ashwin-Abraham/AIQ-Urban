using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions;
using AIQ.Urban.RhinoPlugin.HTML_UI.MessageToUI;
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
        var use = DefaultUses.MixedUse;

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

        var gatherStep = new SelectBuildingFootprintsStep(selected, FloorHeight, FloorCount, use, "Footprint");
        var creationData = gatherStep.Run(doc);
        var idsToDelete = creationData.OfType<BuildingData>().Select(b => b.Footprint.Id).ToList();

        return RunPipeline(doc, gatherStep, idsToDelete);
    }

    private ModelStateMessage HandleMassMode(RhinoDoc doc, Use use)
    {
        var selected = doc.Objects.GetSelectedObjects(false, false)
            .Where(obj => obj.ObjectType == ObjectType.Brep)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more building envelope Breps first.");

        var gatherStep = new SelectBuildingFootprintsStep(selected, FloorHeight, FloorCount, use, "Mass");
        var creationData = gatherStep.Run(doc);
        var idsToDelete = creationData.OfType<BuildingData>().Select(b => b.Footprint.Id).ToList();

        return RunPipeline(doc, gatherStep, idsToDelete);
    }

    private ModelStateMessage RunPipeline(RhinoDoc doc, SelectBuildingFootprintsStep gatherStep, List<Guid> idsToDelete)
    {
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(gatherStep)
            .WithBuildingAdditionSteps([], idsToDelete)
            .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

        if (Urban_RhinoPlugin.Instance.EventHandler is { } handler)
            pipeline.WithEventHandler(handler);

        pipeline.Run();
        doc.Views.Redraw();

        return new ModelStateMessage(UrbanModel.Instance);
    }
}
