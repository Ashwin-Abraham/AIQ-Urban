using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Pipeline.Configs;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace AIQ.Urban.RhinoInterface;

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
        var useNames = DefaultUses.Names;
        var useListIndex = go.AddOptionList("Use", useNames, 0);
        int selectedUseIndex = 0;

        go.AddOptionDouble("Height", ref floorHeightOption);
        go.AddOptionInteger("Count", ref floorCountOption);

        if (!TryRunSelectionLoop(go, useListIndex, ref selectedUseIndex))
            return Result.Cancel;

        var selectedUse = DefaultUses.All[selectedUseIndex];
        var floorHeight = floorHeightOption.CurrentValue;
        var floorCount = floorCountOption.CurrentValue;
        var curveRef = go.Object(0);
        var footprint = curveRef.Curve();

        if (footprint == null)
        {
            RhinoApp.WriteLine("Selected object is not a curve.");
            return Result.Failure;
        }

        Polygon2D profile= new Polygon2D(footprint);
        var buildingData = new BuildingData(profile, floorHeight, floorCount, selectedUse);
        var config = new BuildingPipelineConfiguration([buildingData], [curveRef.ObjectId]);

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
        new PipelineConfigurations(pipeline)
            .WithBuildingAdditionSteps(config)
            .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name);
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
