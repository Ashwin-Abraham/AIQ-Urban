using System.Drawing;
using Rhino.DocObjects;

namespace AIQ.GH.Exports.Types;

public class LayerConfiguration
{
    public string LayerName { get; }
    public bool IsVisible { get; }
    public Color DisplayColor { get; }
    public Color PrintColor { get; }
    public bool IsLocked { get; }

    public LayerConfiguration(
        string layerName,
        bool isVisible = true,
        Color? displayColor = null,
        Color? printColor = null,
        bool isLocked = false)
    {
        LayerName = layerName;
        IsVisible = isVisible;
        DisplayColor = displayColor ?? Color.FromArgb(255, 0, 0, 0);
        PrintColor = printColor ?? Color.FromArgb(255, 0, 0, 0);
        IsLocked = isLocked;
    }

    public static LayerConfiguration FromRhinoLayer(Layer layer)
    {
        return new LayerConfiguration(
            layer.FullPath,
            layer.IsVisible,
            layer.Color,
            layer.PlotColor,
            layer.IsLocked
        );
    }

    public void ApplyToRhinoLayer(Layer layer)
    {
        layer.IsVisible = IsVisible;
        layer.Color = DisplayColor;
        layer.PlotColor = PrintColor;
        layer.IsLocked = IsLocked;
    }

    public string ToJson()
    {
        return $"{LayerName}|{IsVisible}|{DisplayColor.ToArgb()}|{PrintColor.ToArgb()}|{IsLocked}";
    }

    public static LayerConfiguration? FromJson(string json)
    {
        try
        {
            var parts = json.Split('|');
            if (parts.Length < 5) return null;

            var name = parts[0];
            var visible = bool.Parse(parts[1]);
            var displayColor = Color.FromArgb(int.Parse(parts[2]));
            var printColor = Color.FromArgb(int.Parse(parts[3]));
            var locked = bool.Parse(parts[4]);

            return new LayerConfiguration(name, visible, displayColor, printColor, locked);
        }
        catch
        {
            return null;
        }
    }
}
