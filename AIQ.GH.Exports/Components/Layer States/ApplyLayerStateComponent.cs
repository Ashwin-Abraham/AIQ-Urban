using System;
using System.Drawing;
using System.Linq;
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
               "AIQ", "Layer States")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.ApplyLayerState;

    protected override Bitmap Icon => IconLoader.LoadIcon("WriteLayerState.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Doc Layer State", "DL", "Document layer state to apply", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Run", "R", "Apply the layer state to the document", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddBooleanParameter("Success", "S", "True if applied successfully", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        bool run = false;
        if (!DA.GetData<bool>(1, ref run) || !run)
            return;

        var doc = RhinoDoc.ActiveDoc;
        if (doc is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document");
            DA.SetData(0, false);
            return;
        }

        DocLayerStateGoo? goo = null;
        if (!DA.GetData(0, ref goo) || goo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid DocLayerState input");
            DA.SetData(0, false);
            return;
        }

        var layerState = goo.Value;

        layerState.ApplyTo(doc);

        DA.SetData(0, true);
    }
}
