using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions.EditData;
using AIQ.Urban.Core.Models;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Logging;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDataGatherSteps;
using Microsoft.Extensions.Logging;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;

namespace AIQ.Urban.RhinoPlugin;

public class AddBuilding_AIQUrban : Command
{
    public AddBuilding_AIQUrban()
    {
        Instance = this;
    }

    public static AddBuilding_AIQUrban Instance { get; private set; }

    public override string EnglishName => "AddBuilding_AIQUrban";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var go = new GetObject();
        go.SetCommandPrompt("Select building footprint curve");
        go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;

        var floorHeightOption = new OptionDouble(3.0, 0.1, 100.0);
        var floorCountOption = new OptionInteger(1, 1, 1000);
        var useNames = UrbanModel.Instance.Uses.Select(u => u.Name).ToArray();
        var useListIndex = go.AddOptionList("Use", useNames, 0);
        int selectedUseIndex = 0;

        go.AddOptionDouble("Height", ref floorHeightOption);
        go.AddOptionInteger("Count", ref floorCountOption);

        if (!TryRunSelectionLoop(go, useListIndex, ref selectedUseIndex))
            return Result.Cancel;

        var selectedUse = UrbanModel.Instance.Uses[selectedUseIndex];
        var floorHeight = floorHeightOption.CurrentValue;
        var floorCount = floorCountOption.CurrentValue;
        var curveRef = go.Object(0);
        var rhinoObj = curveRef.Object();

        if (rhinoObj is null)
        {
            RhinoApp.WriteLine("Selected object is no longer valid.");
            return Result.Failure;
        }

        var rhinoObjs = new[] { rhinoObj };

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var gatherStep = new SelectBuildingFootprintStep(rhinoObjs, floorHeight, floorCount, selectedUse, loggerFactory);
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, loggerFactory)
            .WithDataGatherStep(gatherStep)
            .WithBuildingAddOrUpdateSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name)
            .WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);

        pipeline.Run();

        doc.Views.Redraw();
        RhinoApp.WriteLine("Added building: {0} storeys, total height={1}, use={2}.",
            floorCount, floorHeight * floorCount, selectedUse.Name);
        return Result.Success;
    }

    private static bool TryRunSelectionLoop(GetObject go, int useListIndex, ref int selectedUseIndex)
    {
        while (true)
        {
            var rc = go.Get();

            if (rc == GetResult.Option)
            {
                if (go.Option().Index == useListIndex)
                    selectedUseIndex = go.Option().CurrentListOptionIndex;
                continue;
            }

            if (rc != GetResult.Object)
                return false;

            break;
        }

        return true;
    }
}
