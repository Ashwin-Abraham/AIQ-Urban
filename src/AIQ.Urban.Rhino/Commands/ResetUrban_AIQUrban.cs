using System;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;

namespace AIQ.Urban.RhinoPlugin;

public class ResetUrban_AIQUrban : Command
{
    public ResetUrban_AIQUrban()
    {
        Instance = this;
    }

    public static ResetUrban_AIQUrban Instance { get; private set; }

    public override string EnglishName => "ResetUrban_AIQUrban";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var gs = new GetString();
        gs.SetCommandPrompt("Type 'yes' to confirm reset of all urban geometry");
        gs.Get();

        if (gs.CommandResult() != Result.Success)
            return gs.CommandResult();

        if (!gs.StringResult().Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            RhinoApp.WriteLine("Reset cancelled.");
            return Result.Cancel;
        }

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance);
        pipeline.WithUrbanElementStep(new ResetUrbanElementsStep());
        pipeline.WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);
        pipeline.Run();

        doc.Views.Redraw();
        RhinoApp.WriteLine("Urban model reset. All geometry deleted.");
        return Result.Success;
    }
}
