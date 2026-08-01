using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions.CreationData;
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
        var creationData = gatherStep.Run(doc);
        var idsToDelete = creationData.OfType<BuildingFootprintData>().Select(b => b.Footprint.Id).ToList();

        return RunPipeline(doc, creationData, idsToDelete, loggerFactory);
    }

    private ModelStateMessage HandleMassMode(RhinoDoc doc, Use use)
    {
        var selected = doc.Objects.GetSelectedObjects(false, false)
            .Where(obj => obj.ObjectType is ObjectType.Brep or ObjectType.Extrusion)
            .ToList();

        if (selected.Count == 0)
            throw new Exception("Select one or more building envelope Breps or Extrusions first.");

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var gatherStep = new SelectBuildingEnvelopeStep(selected, FloorHeight, use, loggerFactory);
        var selectedEnvelopeData = gatherStep.Run(doc).OfType<BuildingMassData>().ToList();
        var resolver = new BuildingEnvelopeCreationDataResolver(UrbanModel.Instance);
        var creationData = resolver.Resolve(selectedEnvelopeData);
        var idsToDelete = selectedEnvelopeData.Select(data => data.Envelope.Id).ToList();

        return RunPipeline(doc, creationData, idsToDelete, loggerFactory);
    }

    private ModelStateMessage RunPipeline(RhinoDoc doc, List<ICreationData> creationData, List<Guid> idsToDelete, ILoggerFactory loggerFactory)
    {
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, loggerFactory)
            .WithCreationData(creationData)
            .WithBuildingAddOrUpdateSteps([], idsToDelete)
            .WithFullModelOutput()
            .WithMetricsCalculation(tolerance: doc.ModelAbsoluteTolerance);

        if (Urban_RhinoPlugin.Instance.EventHandler is { } handler)
            pipeline.WithEventHandler(handler);

        pipeline.Run();
        doc.Views.Redraw();

        return new ModelStateMessage(UrbanModel.Instance);
    }
}
