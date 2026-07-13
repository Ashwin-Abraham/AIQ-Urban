using System;
using System.Collections.Generic;
using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models;
using AIQ.Urban.RhinoInterface;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace AIQ.Urban.RhinoPlugin;

public class ChangeBuildingUse_AIQUrban : Command
{
    public ChangeBuildingUse_AIQUrban()
    {
        Instance = this;
    }

    public static ChangeBuildingUse_AIQUrban Instance { get; private set; }

    public override string EnglishName => "ChangeBuildingUse_AIQUrban";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var go = new GetObject();
        go.SetCommandPrompt("Select building envelopes (Breps) and/or floor plate outlines (curves)");
        go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve | Rhino.DocObjects.ObjectType.Brep;
        go.EnablePreSelect(false, true);
        go.SubObjectSelect = false;
        go.GroupSelect = true;

        var useNames = UrbanModel.Instance.Uses.Select(u => u.Name).ToArray();
        var useListIndex = go.AddOptionList("Use", useNames, 0);
        int selectedUseIndex = 0;

        if (!TryRunSelectionLoop(go, useListIndex, ref selectedUseIndex))
            return Result.Cancel;

        if (go.ObjectCount == 0)
        {
            RhinoApp.WriteLine("No objects selected.");
            return Result.Cancel;
        }

        var selectedUse = UrbanModel.Instance.Uses[selectedUseIndex];
        var geometryIds = new List<Guid>(go.ObjectCount);

        for (int i = 0; i < go.ObjectCount; i++)
            geometryIds.Add(go.Object(i).ObjectId);

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithBuildingUseChangeSteps(geometryIds, selectedUse)
            .WithFullModelOutput()
            .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name)
            .WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);

        pipeline.Run();

        doc.Views.Redraw();
        RhinoApp.WriteLine("Changed {0} selected building(s) to use: {1}.",
            go.ObjectCount, selectedUse.Name);
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
