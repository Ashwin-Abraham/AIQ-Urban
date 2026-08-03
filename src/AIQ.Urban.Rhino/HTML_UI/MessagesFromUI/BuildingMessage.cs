using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Logging;
using AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;
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
        var doc = RhinoDoc.ActiveDoc ?? throw new Exception("No active Rhino document.");
        var selectedIds = doc.Objects.GetSelectedObjects(false, false).Select(x => x.Id).ToList();
        var mode = manual_generationType == "Mass"
            ? BuildingInputMode.Mass
            : BuildingInputMode.Footprint;

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var preparationStep = new AddOrUpdateBuildingInputPreparationStep(
            selectedIds,
            FloorHeight,
            FloorCount,
            DefaultUses.UndefinedName,
            mode);
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, preparationStep, loggerFactory)
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
