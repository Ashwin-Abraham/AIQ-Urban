using System;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Logging;
using AIQ.Urban.RhinoInterface.Pipeline.InputPreparation;
using Microsoft.Extensions.Logging;
using Rhino;
using Rhino.Commands;
using Rhino.Input.Custom;

namespace AIQ.Urban.RhinoPlugin;

public class DeleteAllUrbanElements_AIQUrban : Command
{
    public DeleteAllUrbanElements_AIQUrban()
    {
        Instance = this;
    }

    public static DeleteAllUrbanElements_AIQUrban Instance { get; private set; }

    public override string EnglishName => "DeleteAllUrbanElements_AIQUrban";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var gs = new GetString();
        gs.SetCommandPrompt("Type 'yes' to confirm deletion of all urban elements");
        gs.Get();

        if (gs.CommandResult() != Result.Success)
            return gs.CommandResult();

        if (!gs.StringResult().Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            RhinoApp.WriteLine("Delete all cancelled.");
            return Result.Cancel;
        }

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var preparationStep = new DeleteAllUrbanElementsInputPreparationStep();
        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, preparationStep, loggerFactory);
        pipeline.WithDeleteAllUrbanElementsSteps();
        pipeline.WithFullModelOutput();
        pipeline.WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);
        pipeline.Run();

        doc.Views.Redraw();
        RhinoApp.WriteLine("All urban elements and their geometry were deleted.");
        return Result.Success;
    }
}
