using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.Core.Models;
using AIQ.Urban.RhinoPlugin.Utils;
using AIQ.Urban.RhinoPlugin.Extensions;

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

    /// <summary>Runs the command: user sees default width/type on command line, selects curves, outlines baked.</summary>
    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var tolerance = doc.ModelAbsoluteTolerance;

        var go = new GetObject();
        go.SetCommandPrompt("Select centerline curves");
        go.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;

        var widthOption = new OptionDouble(7.0, 0.001, double.MaxValue);
        var typeOption = new OptionInteger(0, 0, 4);
        go.AddOptionDouble("Width", ref widthOption);
        go.AddOptionInteger("Type", ref typeOption);

        while (true)
        {
            var rc = go.GetMultiple(1, 0);
            if (rc == GetResult.Option)
                continue;
            if (rc != GetResult.Object)
                return go.CommandResult();
            break;
        }

        var width = widthOption.CurrentValue;
        var transitType = (TransitType)typeOption.CurrentValue;
        int added = 0;

        for (int i = 0; i < go.ObjectCount; i++)
        {
            var curveRef = go.Object(i);
            var centerline = curveRef.Curve();
            if (centerline is null)
                continue;

            if (!TryValidateCenterline(centerline, tolerance, out var error))
            {
                RhinoApp.WriteLine("Skipping curve {0}: {1}", i + 1, error);
                continue;
            }

            var transit = new Transit(centerline, width, transitType, tolerance: tolerance);
            UrbanModel.Instance.AddTransit(transit);

            var layerIndex = doc.Layers.EnsureLayerExists(LayerConstants.Transit).Index;
            var attributes = new Rhino.DocObjects.ObjectAttributes() { LayerIndex = layerIndex };
            doc.Objects.AddCurve(transit.Outline, attributes);
            added++;
        }

        doc.Views.Redraw();
        RhinoApp.WriteLine("Added {0} transit corridor(s), width={1}, type={2}.", added, width, transitType);
        return Result.Success;
    }

    private static bool TryValidateCenterline(Curve curve, double tolerance, out string error)
    {
        if (!curve.TryGetPlane(out var plane))
        {
            error = "Curve must be planar.";
            return false;
        }

        if (!curve.IsPlanar(tolerance))
        {
            error = "Curve is not planar within tolerance.";
            return false;
        }

        var bbox = curve.GetBoundingBox(plane);
        if (System.Math.Abs(bbox.Max.Z - bbox.Min.Z) > tolerance)
        {
            error = string.Format("Z variation ({0}) exceeds tolerance.", System.Math.Abs(bbox.Max.Z - bbox.Min.Z));
            return false;
        }

        error = null;
        return true;
    }
}
