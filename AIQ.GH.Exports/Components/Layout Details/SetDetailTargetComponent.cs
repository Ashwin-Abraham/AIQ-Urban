using System;
using System.Drawing;
using System.Linq;
using AIQ.GH.Exports.Enums;
using AIQ.GH.Exports.Extensions;
using AIQ.GH.Exports.Utils;
using Grasshopper.Kernel;
using Rhino;

namespace AIQ.GH.Exports.Components;

public class SetDetailTargetComponent : GH_Component
{
    public SetDetailTargetComponent()
        : base("Set Detail Target", "SetTarget",
               "Sets the camera target and scale for a detail view",
               "AIQ", "Layout Details")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.SetDetailTarget;

    protected override Bitmap Icon => IconLoader.LoadIcon("Set Detail Target.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Layout Name", "L", "Name of the layout containing the detail", GH_ParamAccess.item);
        pManager.AddTextParameter("Detail Name", "D", "Name of the detail view", GH_ParamAccess.item);
        pManager.AddNumberParameter("Scale", "S", "Detail scale as a ratio (e.g., 100 for 1:100)", GH_ParamAccess.item, 100);
        pManager.AddPointParameter("Target Point", "T", "Camera target point in model coordinates", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Anchor Position", "A",
            "Position of target in the detail as integer(0-4):" +
            "\n0 = Top Left" +
            "\n1 = Top Right" +
            "\n2 = Bottom Left" +
            "\n3 = Bottom Right" +
            "\n4 = Center",
            GH_ParamAccess.item,
            0);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddBooleanParameter("Success", "S", "True if target was set successfully", GH_ParamAccess.item);
        pManager.AddTextParameter("Layout Name", "L", "Layout Name Output", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string? layoutName = null;
        string? detailName = null;
        double scale = 100;
        int anchorPosition = 0;
        Rhino.Geometry.Point3d target = Rhino.Geometry.Point3d.Unset;

        if (!DA.GetData(0, ref layoutName)) return;
        if (!DA.GetData(1, ref detailName)) return;
        if (!DA.GetData(2, ref scale)) return;
        if (!DA.GetData(3, ref target)) return;
        if (!DA.GetData(4, ref anchorPosition)) return;

        if (string.IsNullOrWhiteSpace(layoutName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Layout name cannot be empty");
            DA.SetData(0, false);
            return;
        }

        if (string.IsNullOrWhiteSpace(detailName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Detail name cannot be empty");
            DA.SetData(0, false);
            return;
        }

        if (scale <= 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Scale must be greater than 0");
            DA.SetData(0, false);
            return;
        }

        var doc = RhinoDoc.ActiveDoc;
        if (doc is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document");
            DA.SetData(0, false);
            return;
        }

        var layout = doc.FindLayouts(layoutName);
        if (layout is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Layout '{layoutName}' not found");
            DA.SetData(0, false);
            return;
        }

        var detail = layout.FindDetails(detailName).ToList();
        if (detail.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Detail '{detailName}' not found in layout '{layoutName}'");
            DA.SetData(0, false);
            return;
        }

        detail.ForEach(d => d.SetDetailTarget(target, scale, GetAnchorPosition(anchorPosition), doc));
        DA.SetData(0, true);
        DA.SetData(1, layoutName);
    }

    private static AnchorPosition GetAnchorPosition(int anchorPosition)
    {
        return anchorPosition switch
        {
            0 => AnchorPosition.TopLeft,
            1 => AnchorPosition.TopRight,
            2 => AnchorPosition.BottomLeft,
            3 => AnchorPosition.BottomRight,
            4 => AnchorPosition.Center,
            _ => AnchorPosition.TopLeft
        };
    }
}
