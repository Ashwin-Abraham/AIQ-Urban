using System;
using System.Drawing;
using AIQ.GH.Exports.Helpers;
using AIQ.GH.Exports.Models;
using AIQ.GH.Exports.Types;
using Grasshopper.Kernel;
using Rhino;

namespace AIQ.GH.Exports.Components;

public class DocLayerStateFromRhinoLayerState : GH_Component
{
    public DocLayerStateFromRhinoLayerState()
        : base("Doc Layer State From Rhino Layer State", "DocLayerStateRhino",
               "Creates a DocLayerState from the specified layer state object of the Rhino document",
               "AIQ", "Layer States")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.CreateDocLayerStateFromRhinoLayerState;

    protected override Bitmap Icon => IconLoader.LoadIcon("Save LayerState.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter(
            "Name", 
            "N", 
            "Rhino layer states to search for. Type \"LayerState\" in Rhino to see what layer states you currently have in the document",
            GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Doc Layer State", "DL", "Document layer state", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var doc = RhinoDoc.ActiveDoc;
        if (doc is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document");
            return;
        }

        string? layerStateName = null;
        if (!DA.GetData(0, ref layerStateName) || layerStateName == null || layerStateName == string.Empty)
            return;

        var docLayerState = DocLayerState.FromLayerState(doc, layerStateName);

        if(docLayerState != null)
            DA.SetData(0, new DocLayerStateGoo(docLayerState));
    }
}
