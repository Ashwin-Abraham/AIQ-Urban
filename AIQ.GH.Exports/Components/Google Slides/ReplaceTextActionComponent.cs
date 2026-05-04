using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Types;
using AIQ.Exports.GoogleSlides;
using System;
using System.Drawing;
using AIQ.Exports.GoogleSlides.Actions;
using AIQ.Exports.GoogleSlides.Utils;
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
        pManager.AddIntegerParameter("Slide Number", "S", "Slide number (1-based, 0 or empty for all slides)", GH_ParamAccess.item, 0);
        pManager.AddBooleanParameter("Match Case", "M", "True for case-sensitive matching", GH_ParamAccess.item, false);
        pManager.AddTextParameter("Find Text", "Find", "Text to find", GH_ParamAccess.item);
        pManager.AddTextParameter("Replace With", "Replace", "Replacement text", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Action", "A", "Replace text action", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        int slideNumber = 0;
        bool matchCase = false;
        string? findText = null;
        string? replaceText = null;

        DA.GetData(0, ref slideNumber);
        DA.GetData(1, ref matchCase);
        if (!DA.GetData(2, ref findText)) return;
        if (!DA.GetData(3, ref replaceText)) return;

        if (string.IsNullOrWhiteSpace(findText))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Find text cannot be empty");
            return;
        }

        var action = new ReplaceTextAction(
            slideNumber <= 0 ? null : slideNumber,
            matchCase ? TextMatchOption.MatchCase : TextMatchOption.DontMatchCase,
            findText,
            replaceText ?? "");
        DA.SetData(0, new ReplaceTextActionGoo(action));
    }
}