using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Components;
using AIQ.GH.Exports.Models;
using AIQ.GH.Exports.Types;
using Rhino;
using System;
using System.Collections.Generic;
using System.Drawing;
using AIQ.GH.Exports.Helpers;

using System.Linq;

namespace AIQ.GH.Exports.Components;

public class DocLayerStateFromListComponent : GH_Component
{
    public DocLayerStateFromListComponent()
        : base("Doc Layer State From List", "DocLayerStateList",
               "Creates a DocLayerState from lists of layer properties",
               "AIQ", "Layers")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.CreateDocLayerStateFromList;

    protected override Bitmap Icon => IconLoader.LoadIcon("Save LayerState2.png");

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
        manager.AddTextParameter("Full Paths", "FP", "Full paths of layers", GH_ParamAccess.list);
        manager.AddBooleanParameter("Layer On", "LO", "Layer visibility state", GH_ParamAccess.list);
        manager.AddColourParameter("Display Colors", "DC", "Display colors", GH_ParamAccess.list);
        manager.AddNumberParameter("Line Weights", "LW", "Line weights", GH_ParamAccess.list);
        manager.AddTextParameter("Line Types", "LT", "Line type names", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
        manager.AddGenericParameter("Doc Layer State", "DL", "Document layer state", GH_ParamAccess.item);
    }

    private static DocLayerState? CreateFromLists(
        List<string> fullPaths,
        List<bool> layerOn,
        List<Color> displayColors,
        List<double> lineWeights,
        List<string> lineTypes)
    {
        var count = fullPaths.Count;
        if (layerOn.Count != count || displayColors.Count != count || 
            lineWeights.Count != count || lineTypes.Count != count)
        {
            return null;
        }

        return DocLayerState.FromLists(fullPaths, layerOn, displayColors, lineWeights, lineTypes);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        var fullPaths = new List<string>();
        var layerOn = new List<bool>();
        var displayColors = new List<Color>();
        var lineWeights = new List<double>();
        var lineTypes = new List<string>();

        if (!da.GetDataList(0, fullPaths)) return;
        da.GetDataList(1, layerOn);
        da.GetDataList(2, displayColors);
        da.GetDataList(3, lineWeights);
        da.GetDataList(4, lineTypes);

        var docLayerState = CreateFromLists(fullPaths, layerOn, displayColors, lineWeights, lineTypes);
        if (docLayerState is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, 
                "All input lists must have the same length");
            return;
        }

        da.SetData(0, new DocLayerStateGoo(docLayerState));
    }
}
