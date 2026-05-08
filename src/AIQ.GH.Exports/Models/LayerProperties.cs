using System.Drawing;

namespace AIQ.GH.Exports.Models;
public record LayerProperties(
    string FullPath,
    bool? LayerOn,
    Color? LayerDisplayColor,
    double? LayerLineWeight,
    string? LayerLineType,
    Color? LayerPlotColor,
    double? LayerPlotWeight);
