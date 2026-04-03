using System;
using System.Drawing;
using AIQ.GH.Exports.Helpers;
using AIQ.GH.Exports.Types;
using ArchSitePlan.RhinoInterface.Helpers;
using Grasshopper.Kernel;
using Rhino;

namespace AIQ.GH.Exports.Components;

public class ApplyLayerStateComponent : GH_Component
{
    public ApplyLayerStateComponent()
        : base("Apply Layer State", "ApplyLayerState",
               "Applies a DocLayerState to the active Rhino document",
               "AIQ", "Layers")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.ApplyLayerState;

    protected override Bitmap Icon => IconLoader.LoadIcon("WriteLayerState.png");

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
        manager.AddGenericParameter("Doc Layer State", "DL", "Document layer state to apply", GH_ParamAccess.item);
        manager.AddBooleanParameter("Run", "R", "Apply the layer state to the document", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
        manager.AddBooleanParameter("Success", "S", "True if applied successfully", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        bool run = false;
        if (!da.GetData<bool>(1, ref run) || !run)
            return;

        var doc = RhinoDoc.ActiveDoc;
        if (doc is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document");
            da.SetData(0, false);
            return;
        }

        DocLayerStateGoo? goo = null;
        if (!da.GetData(0, ref goo) || goo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid DocLayerState input");
            da.SetData(0, false);
            return;
        }

        var layerState = goo.Value;

        foreach (var props in layerState.LayerState)
        {
            var layerIndex = doc.Layers.FindByFullPath(props.FullPath, -1);
            if (layerIndex < 0)
                continue;

            var layer = doc.Layers[layerIndex];
            LayerHelpers.ApplyLayerProperties(doc, layer, props.LayerOn, props.LayerDisplayColor, props.LayerLineWeight, props.LayerLineType);
        }

        da.SetData(0, true);
    }
}
