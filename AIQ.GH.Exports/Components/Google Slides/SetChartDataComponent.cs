using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Helpers;
using AIQ.GH.Exports.Types;
using AIQ.Exports.GoogleSlides;
using System;
using System.Drawing;
using System.Collections.Generic;
using AIQ.Exports.GoogleSlides.Actions;

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
        pManager.AddGenericParameter("Text Filter", "F", "Text filter for chart element", GH_ParamAccess.item);
        pManager.AddGenericParameter("Series Data", "D", "Series data values", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Action", "A", "Set chart data action (placeholder)", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        TextFilterGoo? filterGoo = null;
        var seriesData = new List<string>();

        if (!DA.GetData(0, ref filterGoo)) return;
        DA.GetDataList(1, seriesData);

        if (filterGoo?.Value is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Text filter is null");
            return;
        }

        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Set Chart Data is a placeholder - actual chart data modification requires Google Sheets API integration");

        var action = new SetChartDataAction(filterGoo.Value, seriesData);
        DA.SetData(0, new SetChartDataActionGoo(action));
    }
}