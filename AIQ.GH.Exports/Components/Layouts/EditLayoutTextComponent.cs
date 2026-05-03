using System;
using System.Drawing;
using System.Linq;
using AIQ.GH.Exports.Extensions;
using AIQ.GH.Exports.Utils;
using Grasshopper.Kernel;
using Rhino;


namespace AIQ.GH.Exports.Components.Layouts;

public class EditLayoutTextComponent : GH_Component
{
    public EditLayoutTextComponent()
        : base("Edit Layout Text", "EditLayoutText",
               "Edits the text content of a text object in a layout",
               "AIQ", "Output")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.EditLayoutText;

    protected override Bitmap Icon => IconLoader.LoadIcon("Edit Layout Text.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Layout Name", "L", "Name of the layout containing the text object", GH_ParamAccess.item);
        pManager.AddTextParameter("Text Object Names", "N", "Name of the text object to edit", GH_ParamAccess.item);
        pManager.AddTextParameter("New Texts", "T", "New text content for the text object", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Layout Name", "L", "Name of the layout", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Success", "S", "True if text was updated successfully", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string? layoutName = null;
        string? textObjectName = null;
        string? newText = null;

        if (!DA.GetData(0, ref layoutName)) return;
        if (!DA.GetData(1, ref textObjectName)) return;
        if (!DA.GetData(2, ref newText)) return;

        if (string.IsNullOrWhiteSpace(layoutName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Layout name cannot be empty");
            return;
        }

        var doc = RhinoDoc.ActiveDoc;
        if (doc is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document");
            DA.SetData(1, false);
            return;
        }

        var layout = doc.FindLayouts(layoutName);
        if (layout is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Layout '{layoutName}' not found");
            DA.SetData(1, false);
            return;
        }

        var textObjects = doc.GetTextObjectsInLayout(layout);

        var textObject = textObjects.FirstOrDefault(t => t.Name == textObjectName);
        if (textObject is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Text object '{textObjectName}' not found in layout '{layoutName}'");
            return;
        }

        var updated = textObject.SetText(newText?? "");

        DA.SetData(0, layoutName);
        DA.SetData(1, updated);
    }
}
