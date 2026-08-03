using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions.CreationData;
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

public class AddZone_AIQUrban : Command
{
    public AddZone_AIQUrban()
    {
        Instance = this;
    }

    public static AddZone_AIQUrban Instance { get; private set; }

    public override string EnglishName => "AddZone_AIQUrban";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var go = new GetObject();
        go.SetCommandPrompt("Select closed curve for zone boundary");
        go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;

        if (go.Get() != GetResult.Object)
            return Result.Cancel;

        var curveRef = go.Object(0);

        var zoneName = string.Empty;
        var rc = RhinoGet.GetString("Zone name", false, ref zoneName);
        if (rc != Result.Success || string.IsNullOrWhiteSpace(zoneName))
            return Result.Cancel;

        using var loggerFactory = new LoggerFactory().AddRhinoLog();

        var preparationStep = new AddZoneInputPreparationStep([curveRef.ObjectId], zoneName);

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance, preparationStep, loggerFactory)
            .WithZoneAdditionSteps()
            .WithFullModelOutput()
            .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name)
            .WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);
        pipeline.Run();

        doc.Views.Redraw();
        RhinoApp.WriteLine("Added zone: {0}", zoneName);
        return Result.Success;
    }
}
