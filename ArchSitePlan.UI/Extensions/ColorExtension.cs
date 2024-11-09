using System.Drawing;

namespace ArchSitePlan.UI.Extensions
{
    public static class ColorExtension
    {
        public static System.Windows.Media.Color ToWindowsMediaColor(this Color color)
        {
            var windowsColor = new System.Windows.Media.Color();
            windowsColor.A = color.A;
            windowsColor.R = color.R;
            windowsColor.G = color.G;
            windowsColor.B = color.B;

            return windowsColor;
        }
        public static System.Windows.Media.Color Dim(this System.Windows.Media.Color colour, double pctg)
        {
            pctg = Math.Clamp(pctg, 0, 1);
            var newColour = new System.Windows.Media.Color();
            newColour.A = colour.A;
            newColour.R = (byte)(colour.R * pctg);
            newColour.G = (byte)(colour.G * pctg);
            newColour.B = (byte)(colour.B * pctg);

            return newColour;
        }
    }
}
