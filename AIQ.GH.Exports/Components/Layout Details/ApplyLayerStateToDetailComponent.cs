using System;
using System.Drawing;
using System.Linq;
using AIQ.GH.Exports.Extensions;
using AIQ.GH.Exports.Types;
using AIQ.GH.Exports.Utils;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;

namespace AIQ.GH.Exports.Components;

public class ApplyLayerStateToDetailComponent : GH_Component
{
    public ApplyLayerStateToDetailComponent()
        : base("Apply Layer State to Detail", "ApplyLayerStateDetail",
               "Applies a DocLayerState to a detail view in a layout",
               "AIQ", "Layout Details")
    { }

    public override Guid ComponentGuid => ComponentGuids.ApplyLayerStateToDetail;

    protected override Bitmap Icon => IconLoader.LoadIcon("ApplyLayerStateToDetail.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Detail", "D", "The detail to modify", GH_ParamAccess.item);
        pManager.AddGenericParameter("Doc Layer State", "DL", "DocLayerState to apply", GH_ParamAccess.item);
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

        DetailViewObject? detail = null;
        if (!DA.GetData(0, ref detail))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Detail is required");
            DA.SetData(0, false);
            return;
        }
        
        DocLayerStateGoo? goo = null;
        if (!DA.GetData(1, ref goo) || goo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid DocLayerState input");
            DA.SetData(0, false);
            return;
        }

        if(detail != null)
            goo.Value.ApplyTo(detail, doc);

        DA.SetData(0, detail!= null);
    }
}