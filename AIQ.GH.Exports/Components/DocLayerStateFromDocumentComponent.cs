using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Components;
using AIQ.GH.Exports.Models;
using AIQ.GH.Exports.Types;
using Rhino;
using System;
using System.Collections.Generic;
using System.Drawing;
using AIQ.GH.Exports.Helpers;

using System.Linq;

namespace AIQ.GH.Exports.Components;

public class DocLayerStateFromDocumentComponent : GH_Component
{
    public DocLayerStateFromDocumentComponent()
        : base("Doc Layer State From Document", "DocLayerStateDoc",
               "Creates a DocLayerState from the active Rhino document",
               "AIQ", "Layers")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.CreateDocLayerStateFromDocument;

    protected override Bitmap Icon => IconLoader.LoadIcon("Save LayerState.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
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

        var docLayerState = DocLayerState.FromDocument(doc);
        DA.SetData(0, new DocLayerStateGoo(docLayerState));
    }
}
