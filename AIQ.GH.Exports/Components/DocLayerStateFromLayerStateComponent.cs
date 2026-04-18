using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Components;
using AIQ.GH.Exports.Helpers;
using AIQ.GH.Exports.Models;
using AIQ.GH.Exports.Types;
using Rhino;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rhino.DocObjects.Tables;

namespace AIQ.GH.Exports.Components;

public class DocLayerStateFromLayerStateComponent : GH_Component
{
    public DocLayerStateFromLayerStateComponent()
        : base("Doc Layer State From Layer State", "DocLayerStateLayerState",
               "Creates a DocLayerState from a named layer state in the document",
               "AIQ", "Layers")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.CreateDocLayerStateFromLayerState;

    protected override Bitmap Icon => IconLoader.LoadIcon("Save LayerState.png");

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
        manager.AddTextParameter("Layer State Name", "LS", "Name of the named layer state", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
        manager.AddGenericParameter("Doc Layer State", "DL", "Document layer state", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        string? stateName = null;
        if (!da.GetData(0, ref stateName) || string.IsNullOrEmpty(stateName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid layer state name");
            return;
        }

        var doc = RhinoDoc.ActiveDoc;
        if (doc is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document");
            return;
        }

        var layerStates = doc.NamedLayerStates;
        var stateIndex = layerStates.FindName(stateName);
        if (stateIndex < 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Layer state '{stateName}' not found");
            return;
        }

        var docLayerState = DocLayerState.FromLayerState(doc, stateName);
        if (docLayerState is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to create DocLayerState from layer state");
            return;
        }

        da.SetData(0, new DocLayerStateGoo(docLayerState));
    }
}