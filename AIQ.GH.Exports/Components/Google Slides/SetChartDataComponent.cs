using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Types;
using AIQ.Exports.GoogleSlides;
using System;
using System.Drawing;
using System.Collections.Generic;
using AIQ.Exports.GoogleSlides.Actions;
using AIQ.Exports.GoogleSlides.Utils;
using AIQ.GH.Exports.Utils;

namespace AIQ.GH.Exports.Components.GoogleSlides;
public class SetChartDataComponent : GH_Component
{
    public SetChartDataComponent()
        : base("Set Chart Data", "SetChartData",
               "Creates an action to set chart data (placeholder - not fully implemented)",
               "AIQ", "Google Slides")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.SetChartDataAction;

    protected override Bitmap Icon => IconLoader.LoadIcon("Set Chart Data.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddIntegerParameter("Slide Number", "S", "Slide number (1-based, 0 or empty for all slides)", GH_ParamAccess.item, 0);
        pManager.AddTextParameter("Text Element Name", "T", "Name of the text box element to find", GH_ParamAccess.item);
        pManager.AddBooleanParameter("Match Case", "M", "True for case-sensitive matching", GH_ParamAccess.item, false);
        pManager.AddGenericParameter("Series Data", "D", "Series data values", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Action", "A", "Set chart data action (placeholder)", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        int slideNumber = 0;
        string? textElementName = null;
        bool matchCase = false;
        var seriesData = new List<string>();

        DA.GetData(0, ref slideNumber);
        if (!DA.GetData(1, ref textElementName)) return;
        DA.GetData(2, ref matchCase);
        DA.GetDataList(3, seriesData);

        if (string.IsNullOrWhiteSpace(textElementName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Text element name cannot be empty");
            return;
        }

        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Set Chart Data is a placeholder - actual chart data modification requires Google Sheets API integration");

        var action = new SetChartDataAction(
            slideNumber <= 0 ? null : slideNumber,
            textElementName,
            matchCase ? TextMatchOption.MatchCase : TextMatchOption.DontMatchCase,
            seriesData);
        DA.SetData(0, new SetChartDataActionGoo(action));
    }
}