using System.Collections.Generic;
using System.Drawing;
using Rhino;
using AIQ.GH.Exports.Models;
using ArchSitePlan.RhinoInterface.Helpers;
using System.Linq;

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
                lineTypeName,
                layer.PlotColor,
                layer.PlotWeight
            ));
        }
        return new DocLayerState(layers);
    }

    public static DocLayerState FromLists(
        List<string> fullPaths,
        List<bool?> layerOn,
        List<Color?> displayColors,
        List<double?> lineWeights,
        List<string?> lineTypes,
        List<Color?> plotColors = null,
        List<double?> plotWeights = null)
    {
        var layers = new List<LayerProperties>();
        for (int i = 0; i < fullPaths.Count; i++)
        {
            var layerOnValue = layerOn.Count - 1 > i? layerOn[i] : null;
            var displayColor = displayColors.Count - 1 > i? displayColors[i] : null;
            var lineWeight = lineWeights.Count - 1 > i? lineWeights[i] : null;
            var lineType = lineTypes.Count - 1 > i? lineTypes[i] : null;
            var plotColor = plotColors?.Count > i ? plotColors[i] : null;
            var plotWeight = plotWeights?.Count > i ? plotWeights[i] : null;


            layers.Add(new LayerProperties(
                fullPaths[i],
                layerOnValue,
                displayColor,
                lineWeight,
                lineType,
                plotColor,
                plotWeight
            ));
        }
        return new DocLayerState(layers);
    }

    public void ApplyTo(RhinoDoc doc, bool withRedraw = true)
    {
        var layerProperties = LayerState.OrderBy(x => x.FullPath).ToList();

        foreach (var props in layerProperties)
        {
            var layerIndex = doc.Layers.FindByFullPath(props.FullPath, -1);
            if (layerIndex >= 0)
            {
                var layer = doc.Layers[layerIndex];
                LayerHelpers.ApplyLayerProperties(
                    doc, 
                    layer,
                    props.LayerOn,
                    props.LayerDisplayColor,
                    props.LayerLineWeight,
                    props.LayerLineType);
            }
        }

        if(withRedraw)
            doc.Views.Redraw();
    }
}
