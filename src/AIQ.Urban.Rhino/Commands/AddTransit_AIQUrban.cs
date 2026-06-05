using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models.GeometryElements;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
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
        var tolerance = doc.ModelAbsoluteTolerance;

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
        var rhinoObjs = centerlineRefs
            .Select(x => x.Object())
            .ToList();

        List<TransitData> transitData = BuildTransitData(rhinoObjs, width, transitType, tolerance);

        var centerlineIds = centerlineRefs.Select(x => x.ObjectId).ToList();

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithTransitAdditionSteps(transitData, centerlineIds)
            .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name)
            .WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);
        
        pipeline.Run();

        doc.Views.Redraw();
        RhinoApp.WriteLine("Added {0} transit corridor(s), width={1}, type={2}.", transitData.Count, width, transitType);
        return Result.Success;
    }

    private static List<TransitData> BuildTransitData(List<RhinoObject> rhinoObjs, double width, TransitType transitType, double tolerance)
    {
        List<TransitData> transitData = [];

        for (int i = 0; i < rhinoObjs.Count; i++)
        {
            var curve2D = TryValidateCenterline(rhinoObjs[i], tolerance, out var error);

            if (curve2D is null)
            {
                RhinoApp.WriteLine("Skipping curve {0}: {1}", i + 1, error);
                continue;
            }

            var data = new TransitData(curve2D, width, transitType);
            transitData.Add(data);
        }

        return transitData;
    }

    private static Curve2D? TryValidateCenterline(RhinoObject rhinoObj, double tolerance, out string error)
    {
        if(rhinoObj.Geometry is not Curve curve)
        {
            error = "Object must be curve";
            return null;
        }

        error = "";
        return new Curve2D(curve, rhinoObj.Id);
    }
}
