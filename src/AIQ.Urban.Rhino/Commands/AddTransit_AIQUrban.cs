using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using AIQ.Urban.Core;
using AIQ.Urban.Core.Enums;
using AIQ.Urban.RhinoPlugin.Utils;
using AIQ.Urban.RhinoPlugin.Extensions;
using System.Collections.Generic;
using System;
using System.Linq;
using Rhino.DocObjects;
using AIQ.Urban.Core.Models.UrbanElements;
using AIQ.Urban.Core.Models.GeometryElements;

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
        var typeOption = new OptionInteger(0, 0, 4);
        go.AddOptionDouble("Width", ref widthOption);
        go.AddOptionEnumList("Type", TransitType.Road);

        while (true)
        {
            var rc = go.GetMultiple(1, (int)1e5);

            if (rc == GetResult.Option)
                continue;
            if (rc != GetResult.Object)
                return go.CommandResult();
            break;
        }

        var width = widthOption.CurrentValue;
        var transitType = (TransitType)typeOption.CurrentValue;
        var centerlineRefs = go.Objects();
        var rhinoObjs = centerlineRefs
            .Select(x => x.Object())
            .ToList();

        var selectedIds = centerlineRefs.Select(x => x.ObjectId).ToList();
        List<Transit> newTransits = [];

        for (int i = 0; i < rhinoObjs.Count; i++)
        {
            var centerline = TryValidateCenterline(rhinoObjs[i], tolerance, out var error);

            if (centerline is null)
            {
                RhinoApp.WriteLine("Skipping curve {0}: {1}", i + 1, error);
                continue;
            }

            var curveElement = new CurveElement(centerline, rhinoObjs[i].Id);
            var transit = new Transit(curveElement, width, transitType, tolerance: tolerance);
            newTransits.Add(transit);
        }

        new RhinoUrbanModelPipeline(doc)
            .RemoveMatchingElements(selectedIds, selectedIds)
            .ChangeInputCurvesLayer(selectedIds, transitType, width)
            .AddTransit(newTransits)
            .BakeNewUrbanElements();

        doc.Views.Redraw();
        RhinoApp.WriteLine("Added {0} transit corridor(s), width={1}, type={2}.", newTransits.Count, width, transitType);
        return Result.Success;
    }

    private static Curve? TryValidateCenterline(RhinoObject rhinoObj, double tolerance, out string error)
    {
        if(rhinoObj.Geometry is not Curve curve)
        {
            error = "Object must be curve";
            return null;
        }

        if (!curve.TryGetPlane(out var plane))
        {
            error = "Curve must be planar.";
            return null;
        }

        if (!curve.IsPlanar(tolerance))
        {
            error = "Curve is not planar within tolerance.";
            return null;
        }

        var bbox = curve.GetBoundingBox(plane);
        if (System.Math.Abs(bbox.Max.Z - bbox.Min.Z) > tolerance)
        {
            error = string.Format("Z variation ({0}) exceeds tolerance.", System.Math.Abs(bbox.Max.Z - bbox.Min.Z));
            return null;
        }

        error = "";
        return curve;
    }
}
