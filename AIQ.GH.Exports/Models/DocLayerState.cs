using System.Collections.Generic;
using System.Drawing;
using Rhino;
using AIQ.GH.Exports.Models;
using ArchSitePlan.RhinoInterface.Helpers;

namespace AIQ.GH.Exports.Models;
public class DocLayerState(List<LayerProperties> _layerState)
{
    public List<LayerProperties> LayerState { get; } = _layerState;

    public static DocLayerState FromDocument(RhinoDoc doc)
    {
        var layers = new List<LayerProperties>();
        foreach (var layer in doc.Layers)
        {
            var lineType = doc.Linetypes[layer.LinetypeIndex];
            var lineTypeName = lineType?.Name ?? "ByLayer";
            layers.Add(new LayerProperties(
                layer.FullPath,
                layer.IsVisible,
                layer.Color,
                layer.PlotWeight,
                lineTypeName
            ));
        }
        return new DocLayerState(layers);
    }

    public static DocLayerState FromLists(
        List<string> fullPaths,
        List<bool> layerOn,
        List<Color> displayColors,
        List<double> lineWeights,
        List<string> lineTypes)
    {
        var count = fullPaths.Count;
        var layers = new List<LayerProperties>();
        for (int i = 0; i < count; i++)
        {
            layers.Add(new LayerProperties(
                fullPaths[i],
                layerOn[i],
                displayColors[i],
                lineWeights[i],
                lineTypes[i]
            ));
        }
        return new DocLayerState(layers);
    }

    public void ApplyTo(RhinoDoc doc)
    {
        foreach (var props in LayerState)
        {
            var layerIndex = doc.Layers.FindByFullPath(props.FullPath, -1);
            if (layerIndex >= 0)
            {
                var layer = doc.Layers[layerIndex];
                LayerHelpers.ApplyLayerProperties(doc, layer, props.LayerOn,
                    props.LayerDisplayColor, props.LayerLineWeight, props.LayerLineType);
            }
        }
    }
}
