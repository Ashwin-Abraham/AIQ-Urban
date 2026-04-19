using System;
using System.Drawing;
using System.Linq;
using AIQ.GH.Exports.Extensions;
using AIQ.GH.Exports.Helpers;
using Grasshopper.Kernel;
using Rhino;

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

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Detail Name", "N", "Name of the detail to modify", GH_ParamAccess.item);
        pManager.AddTextParameter("Layer State Name", "LS", "Name of the named layer state to apply", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddBooleanParameter("Success", "S", "True if applied successfully", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var doc = RhinoDoc.ActiveDoc;
        if (doc is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document");
            DA.SetData(0, false);
            return;
        }

        string detailName = "";
        if (!DA.GetData(0, ref detailName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Detail name is required");
            DA.SetData(0, false);
            return;
        }

        string? layerStateName = null;
        if (!DA.GetData(1, ref layerStateName) || string.IsNullOrEmpty(layerStateName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Layer state name is required");
            DA.SetData(0, false);
            return;
        }

        var layerStates = doc.NamedLayerStates;
        var stateIndex = layerStates.FindName(layerStateName);
        if (stateIndex < 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Layer state '{layerStateName}' not found");
            DA.SetData(0, false);
            return;
        }

        var details = doc.GetDetailViews(detailName);
        bool success = details.All(x => x.ApplyLayerStateToDetail(layerStateName, doc));

        DA.SetData(0, success);
    }
}