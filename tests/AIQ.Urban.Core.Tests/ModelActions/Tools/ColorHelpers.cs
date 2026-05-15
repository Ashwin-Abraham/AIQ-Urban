using System.Drawing;
using AIQ.Urban.Core.Enums;

namespace AIQ.Urban.Core.Tests.ModelActions.Tools;

public static class ColorHelpers
{
    public static Color ColorForTransitType(TransitType type) => type switch
    {
        TransitType.Road => Color.DimGray,
        TransitType.Railway => Color.SaddleBrown,
        TransitType.CyclePath => Color.DarkGreen,
        TransitType.Pedestrian => Color.DarkOliveGreen,
        TransitType.Waterway => Color.DodgerBlue,
        _ => ColorFromString(type.ToString()),
    };

    public static Color ColorFromString(string name)
    {
        uint hash = 2166136261;
        foreach (char c in name)
        {
            hash ^= c;
            hash *= 16777619;
        }
        var r = (byte)((hash & 0xFF) % 200 + 55);
        var g = (byte)(((hash >> 8) & 0xFF) % 200 + 55);
        var b = (byte)(((hash >> 16) & 0xFF) % 200 + 55);
        return Color.FromArgb(r, g, b);
    }
}
