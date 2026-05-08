using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AIQ.GH.Exports.Models;
using AIQ.GH.Exports.Types;
using AIQ.GH.Exports.Utils;
using Grasshopper.Kernel;

namespace AIQ.GH.Exports.Components;

public class DocLayerStateFromListComponent : GH_Component
{
    public DocLayerStateFromListComponent()
        : base("Doc Layer State From List", "DocLayerStateList",
               "Creates a DocLayerState from lists of layer properties",
               "AIQ", "Layer States")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.CreateDocLayerStateFromList;

    protected override Bitmap Icon => IconLoader.LoadIcon("Save LayerState2.png");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Full Paths", "FP", "Full paths of layers", GH_ParamAccess.list);
        pManager.AddBooleanParameter("Layer On", "LO", "Layer visibility state", GH_ParamAccess.list, []);
        pManager.AddColourParameter("Display Colors", "DC", "Display colors", GH_ParamAccess.list, []);
        pManager[2].Optional = true;

        pManager.AddNumberParameter("Line Weights", "LW", "Line weights", GH_ParamAccess.list, []);
        pManager[3].Optional = true;

        pManager.AddTextParameter("Line Types", "LT", "Line type names", GH_ParamAccess.list, []);
        pManager[4].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddGenericParameter("Doc Layer State", "DL", "Document layer state", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var fullPaths = new List<string>();
        var layerOn = new List<bool?>();
        var displayColors = new List<Color?>();
        var lineWeights = new List<double?>();
        var lineTypes = new List<string?>();

        if (!DA.GetDataList(0, fullPaths)) return;
        DA.GetDataList(1, layerOn);
        DA.GetDataList(2, displayColors);
        DA.GetDataList(3, lineWeights);
        DA.GetDataList(4, lineTypes);

        var count = fullPaths.Count;

        displayColors = displayColors.Select(x => EnforceNull(x)).ToList();

        if ((layerOn.Count > 0 && layerOn.Count != count)
            || (displayColors.Count > 0 && displayColors.Count != count)
            || (lineWeights.Count > 0 && lineWeights.Count != count)
            || (lineTypes.Count > 0 && lineTypes.Count != count))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, 
                "All input lists must have the same length");
            return;
        }

        var docLayerState = DocLayerState.FromLists(fullPaths, layerOn, displayColors, lineWeights, lineTypes);
        DA.SetData(0, new DocLayerStateGoo(docLayerState));
    }

    private static Color? EnforceNull(Color? color)
    {
        if (color == null || color.Value.Name.Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;

        return color;
    }

}
