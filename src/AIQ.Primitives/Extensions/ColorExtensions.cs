using System.Drawing;

namespace AIQ.Primitives.Extensions;

public static class ColorExtensions
{
    public static Color Average(this IEnumerable<Color> colors)
    {
        var colorList = colors.ToList();
        var red = Math.Round(colorList.Average(x => x.R));
        var green = Math.Round(colorList.Average(x => x.G));
        var blue = Math.Round(colorList.Average(x => x.B));
        var alpha = Math.Round(colorList.Average(x => x.A));

        return Color.FromArgb((int)alpha, (int)red, (int)green, (int)blue);
    }

    public static Color SetTransparency(this Color color, int alpha)
    {
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    public static Color WithAlpha(this Color color, double alpha)
    {
        var alphaValue = (int)Math.Round(Math.Clamp(alpha, 0, 1) * 255, MidpointRounding.AwayFromZero);
        return Color.FromArgb(alphaValue, color.R, color.G, color.B);
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

    public static Color FromHsl(double h, double s, double l)
    {
        h %= 360;
        s /= 100.0;
        l /= 100.0;

        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
        double m = l - c / 2;

        double r = 0, g = 0, b = 0;

        if (h < 60) (r, g, b) = (c, x, 0);
        else if (h < 120) (r, g, b) = (x, c, 0);
        else if (h < 180) (r, g, b) = (0, c, x);
        else if (h < 240) (r, g, b) = (0, x, c);
        else if (h < 300) (r, g, b) = (x, 0, c);
        else (r, g, b) = (c, 0, x);

        return Color.FromArgb(
            (int)Math.Round((r + m) * 255),
            (int)Math.Round((g + m) * 255),
            (int)Math.Round((b + m) * 255));
    }
}
