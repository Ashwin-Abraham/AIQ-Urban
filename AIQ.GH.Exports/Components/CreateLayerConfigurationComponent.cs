using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using AIQ.GH.Exports.Components;
using AIQ.GH.Exports.Types;
using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIQ.GH.Exports.Components;

public class CreateLayerConfigurationComponent : GH_Component
{
    public CreateLayerConfigurationComponent()
        : base("Create Layer Configuration", "LayerConfig",
               "Captures the visibility and color settings of specified layers",
               "AIQ", "Layers")
    {
    }

    public override Guid ComponentGuid => ComponentGuids.CreateLayerConfiguration;

    protected override System.Drawing.Bitmap Icon => null;

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
        manager.AddTextParameter("Layer Names", "L", "Names of layers to capture (leave empty for all layers)", GH_ParamAccess.list);
        manager.AddBooleanParameter("Include Children", "C", "Include child layers", GH_ParamAccess.item, true);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
        manager.AddGenericParameter("Layer Config", "C", "Layer configuration data", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        var layerNames = new List<string>();
        bool includeChildren = true;

        da.GetDataList(0, layerNames);
        da.GetData(1, ref includeChildren);

        var doc = RhinoDoc.ActiveDoc;
        if (doc is null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document");
            return;
        }

        var configurations = new List<LayerConfiguration>();

        if (layerNames.Count == 0)
        {
            foreach (var layer in doc.Layers)
            {
                if (includeChildren || !layer.FullPath.Contains("::"))
                {
                    configurations.Add(LayerConfiguration.FromRhinoLayer(layer));
                }
            }
        }
        else
        {
            foreach (var name in layerNames)
            {
                var layer = doc.Layers.FindName(name);
                if (layer is not null)
                {
                    configurations.Add(LayerConfiguration.FromRhinoLayer(layer));
                    if (includeChildren)
                    {
                        foreach (var child in doc.Layers.Where(l => l.FullPath.StartsWith(layer.FullPath + "::")))
                        {
                            configurations.Add(LayerConfiguration.FromRhinoLayer(child));
                        }
                    }
                }
            }
        }

        var output = new GH_Structure<LayerConfigurationGoo>();
        foreach (var config in configurations)
        {
            output.Append(new LayerConfigurationGoo(config));
        }

        da.SetDataTree(0, output);
    }
}
