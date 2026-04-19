using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.GH.Exports.Helpers;
using AIQ.GH.Exports.Models;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Display;

namespace AIQ.GH.Exports.Components;

public class ApplyLayerStateToDetailComponent : GH_Component
{
    public ApplyLayerStateToDetailComponent()
        : base("Apply Layer State to Detail", "ApplyLayerStateDetail",
               "Applies a named layer state to a detail view in a layout",
               "AIQ", "Layers")
    { }

    public override Guid ComponentGuid => ComponentGuids.ApplyLayerStateToDetail;

    protected override Bitmap Icon => IconLoader.LoadIcon("Set Detail Layer State.png");

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
        manager.AddTextParameter("Detail Name", "N", "Name of the detail to modify", GH_ParamAccess.item);
        manager.AddTextParameter("Layer State Name", "LS", "Name of the named layer state to apply", GH_ParamAccess.item);
        manager.AddTextParameter("Layout", "L", "The name of the layout. If empty, searches all layouts", GH_ParamAccess.item);

        manager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
        manager.AddBooleanParameter("Success", "S", "True if applied successfully", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        var doc = RhinoDoc.ActiveDoc;
        if (doc is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document");
            da.SetData(0, false);
            return;
        }

        string detailName = "";
        if (!da.GetData(0, ref detailName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Detail name is required");
            da.SetData(0, false);
            return;
        }

        string? layerStateName = null;
        if (!da.GetData(1, ref layerStateName) || string.IsNullOrEmpty(layerStateName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Layer state name is required");
            da.SetData(0, false);
            return;
        }

        string? layoutName = null;
        da.GetData(2, ref layoutName);

        var layerStates = doc.NamedLayerStates;
        var stateIndex = layerStates.FindName(layerStateName);
        if (stateIndex < 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Layer state '{layerStateName}' not found");
            da.SetData(0, false);
            return;
        }

        bool success = ApplyLayerStateToDetail(doc, detailName, layerStateName, layoutName);

        da.SetData(0, success);
    }

    private bool ApplyLayerStateToDetail(
        RhinoDoc doc,
        string detailName,
        string layerStateName,
        string? layoutName)
    {
        var pageViews = doc.Views
            .Where(v => v is RhinoPageView)
            .Cast<RhinoPageView>();

        bool focusOnSingleLayout = !string.IsNullOrEmpty(layoutName);

        var detailViews = focusOnSingleLayout?
            pageViews.Where(x => x.PageName == layoutName).SelectMany(x => x.GetDetailViews()).ToList()
            : pageViews.SelectMany(x => x.GetDetailViews()).ToList();

        if (detailViews.Count == 0)
        {
            string message = focusOnSingleLayout ?
                $"Layout {layoutName} not found"
                : "No layouts found in document";

            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);
            return false;
        }

        detailViews = detailViews.Where(x => x.Name == detailName).ToList();

        if (detailViews.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Detail {detailName} not found");
            return false;
        }

        foreach (var detail in detailViews)
        {
            DetailHelpers.ApplyLayerStateToDetail(detail, layerStateName, doc);
        }

        return true;
    }
}