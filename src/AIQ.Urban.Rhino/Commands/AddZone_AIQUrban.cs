using System;
using System.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Models;
using AIQ.Urban.Core.Models.GeometryElements;
using AIQ.Urban.RhinoPlugin.Pipeline.ModelActions;

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
        var curve = TryValidateBoundaryCurve(curveRef.Curve());
        if (curve is null)
            return Result.Failure;

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

        var boundary = new Polygon2D(curve);
        var zoneData = new ZoneData(boundary, zoneName);
        var addZoneStep = new AddZoneStep([zoneData]);

        new RhinoUrbanModelPipeline(doc, UrbanModel.Instance)
            .WithRhinoObjectDeletion([curveRef.ObjectId])
            .WithUrbanElementAddition(addZoneStep)
            .Run();

        doc.Views.Redraw();
        RhinoApp.WriteLine("Added zone: {0}", zoneName);
        return Result.Success;
    }

    private static Curve? TryValidateBoundaryCurve(Curve? curve)
    {
        if (curve == null)
        {
            RhinoApp.WriteLine("Selected object is not a curve.");
            return null;
        }

        if (!curve.IsClosed)
        {
            RhinoApp.WriteLine("Zone boundary must be a closed curve.");
            return null;
        }

        curve.TryGetPlane(out var plane);
        if (plane.Normal.Z < 0)
        {
            curve = curve.DuplicateCurve();
            curve.Reverse();
        }

        return curve;
    }
}
