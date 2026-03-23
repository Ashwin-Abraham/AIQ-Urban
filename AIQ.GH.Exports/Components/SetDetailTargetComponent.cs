using Grasshopper;
using Grasshopper.Kernel;
using AIQ.GH.Exports.Components;
using AIQ.GH.Exports.Helpers;
using Rhino;
using System;
using System.Linq;

namespace AIQ.GH.Exports.Components;

public class SetDetailTargetComponent : GH_Component
{
    public SetDetailTargetComponent()
        : base("Set Detail Target", "SetTarget",
               "Sets the camera target and scale for a detail view",
               "AIQ", "Layout")
    {
    }

    public override Guid ComponentGuid => new Guid(ComponentGuids.SetDetailTarget);

    protected override System.Drawing.Bitmap Icon => null;

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
        manager.AddTextParameter("Layout Name", "L", "Name of the layout containing the detail", GH_ParamAccess.item);
        manager.AddTextParameter("Detail Name", "D", "Name of the detail view", GH_ParamAccess.item);
        manager.AddNumberParameter("Scale", "S", "Detail scale as a ratio (e.g., 100 for 1:100)", GH_ParamAccess.item, 100);
        manager.AddPointParameter("Target Point", "T", "Camera target point in model coordinates", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
        manager.AddBooleanParameter("Success", "S", "True if target was set successfully", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        string layoutName = null;
        string detailName = null;
        double scale = 100;
        Rhino.Geometry.Point3d target = Rhino.Geometry.Point3d.Unset;

        if (!da.GetData(0, ref layoutName)) return;
        if (!da.GetData(1, ref detailName)) return;
        if (!da.GetData(2, ref scale)) return;
        if (!da.GetData(3, ref target)) return;

        if (string.IsNullOrWhiteSpace(layoutName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Layout name cannot be empty");
            da.SetData(0, false);
            return;
        }

        if (string.IsNullOrWhiteSpace(detailName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Detail name cannot be empty");
            da.SetData(0, false);
            return;
        }

        if (scale <= 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Scale must be greater than 0");
            da.SetData(0, false);
            return;
        }

        var doc = RhinoDoc.ActiveDoc;
        if (doc is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document");
            da.SetData(0, false);
            return;
        }

        var layout = LayoutHelpers.GetLayoutByName(doc, layoutName);
        if (layout is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Layout '{layoutName}' not found");
            da.SetData(0, false);
            return;
        }

        var detail = DetailHelpers.GetDetailByName(layout, detailName);
        if (detail is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Detail '{detailName}' not found in layout '{layoutName}'");
            da.SetData(0, false);
            return;
        }

        DetailHelpers.SetDetailTarget(detail, target, scale, doc);
        da.SetData(0, true);
    }
}
