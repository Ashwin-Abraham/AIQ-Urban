using System.Linq;
using AIQ.Urban.Core;
using AIQ.Urban.Core.ModelActions.CreationData;
using AIQ.Urban.RhinoInterface;
using AIQ.Urban.RhinoInterface.Pipeline.RhinoDocActions;
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
        var rhinoObj = curveRef.Object();

        if (rhinoObj is null)
        {
            RhinoApp.WriteLine("Selected object is no longer valid.");
            return Result.Failure;
        }

        var zoneName = string.Empty;
        var rc = RhinoGet.GetString("Zone name", false, ref zoneName);
        if (rc != Result.Success || string.IsNullOrWhiteSpace(zoneName))
            return Result.Cancel;

        if (zoneName.Contains("::"))
        {
            RhinoApp.WriteLine("Zone name must not contain '::'.");
            return Result.Failure;
        }

        if (UrbanModel.Instance.Zones.Any(z => z.Name == zoneName))
        {
            RhinoApp.WriteLine("A zone named '{0}' already exists.", zoneName);
            return Result.Failure;
        }

        var rhinoObjs = new[] { rhinoObj };

        var gatherStep = new SelectZoneBoundaryStep(rhinoObjs, zoneName);
        var creationData = (CreationDataCollection<ZoneData>)gatherStep.Run(doc);
        var zoneData = creationData.Items;
        var idsToDelete = zoneData.Select(z => z.Boundary.Id).ToList();

        var pipeline = new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithDataGatherStep(gatherStep)
            .WithZoneAdditionSteps([], idsToDelete)
            .WithMetricsCalculation(saveToFile: true, tolerance: doc.ModelAbsoluteTolerance, docName: doc.Name)
            .WithEventHandler(Urban_RhinoPlugin.Instance.EventHandler);
        pipeline.Run();

        doc.Views.Redraw();
        RhinoApp.WriteLine("Added zone: {0}", zoneName);
        return Result.Success;
    }
}
