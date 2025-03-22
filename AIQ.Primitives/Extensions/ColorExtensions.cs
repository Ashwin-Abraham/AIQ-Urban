using System.Drawing;

namespace AIQ.Primitives.Extensions;

public static class ColorExtensions
{
    public static Color SetTransparency(this Color color, int alpha)
    {
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    /// <summary>
    /// Darkens a colour by a ratio (from 0-1)
    /// R,G and B values are proportionally reduced
    /// </summary>
    public static Color Darken(this Color color, double ratio)
    {
        ratio = 1 - Math.Clamp(ratio, 0, 1);

        var red = (int)Math.Clamp(color.R * ratio, 0.001, 254.999);
        var green = (int)Math.Clamp(color.G * ratio, 0.001, 254.999);
        var blue = (int)Math.Clamp(color.B * ratio, 0.001, 254.999);

        return Color.FromArgb(color.A, red, green, blue);
    }

    public static Color ToRGB(this Color color)
    {
        return Color.FromArgb(color.R, color.G, color.B);
    }
}
