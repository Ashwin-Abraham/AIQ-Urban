using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Types;
using AIQ.Exports.GoogleSlides;
using System;
using System.Drawing;
using AIQ.Exports.GoogleSlides.Actions;
using AIQ.GH.Exports.Utils;

namespace AIQ.GH.Exports.Components.GoogleSlides;
public class ReplaceTextActionComponent : GH_Component
{
    public ReplaceTextActionComponent()
        : base("Replace Text Action", "ReplaceText",
               "Creates an action to replace text in Google Slides",
               "AIQ", "Google Slides")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.ReplaceTextAction;

    protected override Bitmap Icon => IconLoader.LoadIcon("Replace Text.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddGenericParameter("Text Filter", "F", "Text filter", GH_ParamAccess.item);
        pManager.AddTextParameter("Find Text", "Find", "Text to find", GH_ParamAccess.item);
        pManager.AddTextParameter("Replace With", "Replace", "Replacement text", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Action", "A", "Replace text action", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        TextFilterGoo? filterGoo = null;
        string? findText = null;
        string? replaceText = null;

        if (!DA.GetData(0, ref filterGoo)) return;
        if (!DA.GetData(1, ref findText)) return;
        if (!DA.GetData(2, ref replaceText)) return;

        if (filterGoo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Text filter is null");
            return;
        }

        if (string.IsNullOrWhiteSpace(findText))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Find text cannot be empty");
            return;
        }

        var action = new ReplaceTextAction(filterGoo.Value, findText, replaceText ?? "");
        DA.SetData(0, new ReplaceTextActionGoo(action));
    }
}