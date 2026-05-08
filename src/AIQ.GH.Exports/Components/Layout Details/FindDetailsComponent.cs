using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.GH.Exports.Enums;
using AIQ.GH.Exports.Extensions;
using AIQ.GH.Exports.Utils;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;

namespace AIQ.GH.Exports.Components;

public class FindDetailsComponent : GH_Component
{
    public FindDetailsComponent()
        : base("Find Details", "FindDetails",
               "Finds details in layouts by name using text matching strategies",
               "AIQ", "Layout Details")
    { }

    public override Guid ComponentGuid => ComponentGuids.FindDetails;

    protected override Bitmap Icon => IconLoader.LoadIcon("Find Detail.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Layout Name", "L", "Layout to search in. If empty, searches all layouts", GH_ParamAccess.item);
        pManager.AddTextParameter("Detail Name", "D", "Detail name to search for. If empty, returns all details in matching layouts", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Text Match Strategy", "M", "Text matching strategy: 0 = Contains, 1 = DoesNotContain, 2 = ExactMatch", GH_ParamAccess.item);

        pManager[0].Optional = true;
        pManager[1].Optional = true;
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Details", "D", "List of matching detail objects", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var doc = RhinoDoc.ActiveDoc;
        if (doc is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document");
            return;
        }

        string? layoutName = null;
        DA.GetData(0, ref layoutName);

        string? detailName = null;
        DA.GetData(1, ref detailName);

        var strategyInt = (int)TextMatchStrategy.ExactMatch;
        if (!DA.GetData(2, ref strategyInt))
        {
            strategyInt = (int)TextMatchStrategy.ExactMatch;
        }

        var strategy = (TextMatchStrategy)strategyInt;
        var layout = doc.FindLayouts(layoutName, strategy);
        
        if (layout == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No layouts found matching criteria");
            DA.SetDataList(0, new List<DetailViewObject>());
            return;
        }

        var details = layout.FindDetails(detailName, strategy).ToList();

        if (details.Count == 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No details found matching criteria");
        }

        DA.SetDataList(0, details);
    }
}