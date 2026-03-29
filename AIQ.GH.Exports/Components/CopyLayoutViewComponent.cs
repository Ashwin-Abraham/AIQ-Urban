using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Components;
using AIQ.GH.Exports.Helpers;
using AIQ.GH.Exports.Types;
using Rhino;
using System;
using System.Collections.Generic;
using System.Drawing;


namespace AIQ.GH.Exports.Components;

public class CopyLayoutViewComponent : GH_Component
{
    public CopyLayoutViewComponent()
        : base("Copy Layout View", "CopyLayout",
               "Creates a new layout by copying details from a template layout",
               "AIQ", "Layout")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.CopyLayoutView;

    protected override Bitmap Icon => IconLoader.LoadIcon("Copy Layout.png");

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
        manager.AddTextParameter("Template Layout", "T", "Name of the template layout to copy from", GH_ParamAccess.item);
        manager.AddTextParameter("New Layout Name", "N", "Name for the new layout", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
        manager.AddTextParameter("Layout Name", "L", "Name of the created layout", GH_ParamAccess.item);
        manager.AddBooleanParameter("Success", "S", "True if layout was created successfully", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        string templateName = null;
        string newLayoutName = null;

        if (!da.GetData(0, ref templateName)) return;
        if (!da.GetData(1, ref newLayoutName)) return;

        if (string.IsNullOrWhiteSpace(templateName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Template layout name cannot be empty");
            return;
        }

        if (string.IsNullOrWhiteSpace(newLayoutName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "New layout name cannot be empty");
            return;
        }

        var doc = RhinoDoc.ActiveDoc;
        if (doc is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document");
            da.SetData(1, false);
            return;
        }

        var templateLayout = LayoutHelpers.GetLayoutByName(doc, templateName);
        if (templateLayout is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Template layout '{templateName}' not found");
            da.SetData(1, false);
            return;
        }

        var newLayout = LayoutHelpers.CreateLayoutFromTemplate(doc, templateName, newLayoutName);

        if (newLayout is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to create layout");
            da.SetData(1, false);
            return;
        }

        da.SetData(0, newLayoutName);
        da.SetData(1, true);
    }
}
