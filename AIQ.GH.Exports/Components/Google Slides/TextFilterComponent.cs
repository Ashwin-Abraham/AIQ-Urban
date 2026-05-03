using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Types;
using System;
using System.Drawing;
using AIQ.Exports.GoogleSlides.Filters;
using AIQ.GH.Exports.Utils;

namespace AIQ.GH.Exports.Components.GoogleSlides;
public class TextFilterComponent : GH_Component
{
    public TextFilterComponent()
        : base("Text Filter", "TextFilter",
               "Creates a filter for text elements in Google Slides",
               "AIQ", "Google Slides")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.TextFilter;

    protected override Bitmap Icon => IconLoader.LoadIcon("Text Filter.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddIntegerParameter("Slide Number", "S", "Slide number (1-based, 0 or empty for all slides)", GH_ParamAccess.item, 0);
        pManager.AddTextParameter("Text Element Name", "T", "Name of the text box element to find", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Match Case", "M", "True for case-sensitive matching", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Filter", "F", "Text filter", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        int slideNumber = 0;
        string? textElementName = null;
        bool matchCase = false;

        DA.GetData(0, ref slideNumber);
        if (!DA.GetData(1, ref textElementName)) return;
        DA.GetData(2, ref matchCase);

        if (string.IsNullOrWhiteSpace(textElementName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Text element name cannot be empty");
            return;
        }

        var filter = new TextFilter(
            slideNumber <= 0 ? null : slideNumber,
            textElementName,
            matchCase ? TextMatchOption.MatchCase : TextMatchOption.DontMatchCase);

        DA.SetData(0, new TextFilterGoo(filter));
    }
}