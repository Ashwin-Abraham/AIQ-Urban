using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Logging;
using AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using Microsoft.Extensions.Logging;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;

namespace AIQ.Urban.RhinoPlugin;

/// <summary>Rhino command: pick centerline curves → width + transit type on command line → bake outlines.</summary>
public class AddTransit_AIQUrban : Command
{
    public AddTransit_AIQUrban()
    {
        Instance = this;
    }

    /// <summary>The only instance of this command.</summary>
    public static AddTransit_AIQUrban Instance { get; private set; }

    /// <summary>Command name as it appears on the Rhino command line.</summary>
    public override string EnglishName => "AddTransit_AIQUrban";

    /// <summary>Runs the command: user selects curves, pipes through RhinoUrbanModelPipeline.</summary>
    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var go = new GetObject();
        go.SetCommandPrompt("Select centerline curves");
        go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;

        var widthOption = new OptionDouble(7.0, 0.001, double.MaxValue);
        go.AddOptionDouble("Width", ref widthOption);
        var typeOptionIndex = go.AddOptionEnumList("Type", TransitType.Road);
        var transitType = TransitType.Road;

        while (true)
        {
            var rc = go.GetMultiple(1, (int)1e5);

            if (rc == GetResult.Option)
            {
                if (go.Option().Index == typeOptionIndex)
                    transitType = (TransitType)go.Option().CurrentListOptionIndex;
                continue;
            }
            if (rc != GetResult.Object)
                return go.CommandResult();
            break;
        }

        var width = widthOption.CurrentValue;
        var centerlineRefs = go.Objects();
        var centerlineIds = centerlineRefs.Select(x => x.ObjectId).ToList();

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var preparationStep = new AddTransitInputPreparationStep(centerlineIds, width, transitType);
        var transitCountBefore = UrbanModel.Instance.Transits.Count;

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, preparationStep, loggerFactory)
            .WithTransitAdditionSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name)
            .WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);

        pipeline.Run();

        doc.Views.Redraw();
        var addedCount = Math.Max(0, UrbanModel.Instance.Transits.Count - transitCountBefore);
        RhinoApp.WriteLine("Added {0} transit corridor(s), width={1}, type={2}.", addedCount, width, transitType);
        return Result.Success;
    }
}
