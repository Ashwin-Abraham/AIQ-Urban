using System.Drawing;

namespace ArchSitePlan.UI.Extensions
{
    public static class ColorExtension
    {
        public static System.Windows.Media.Color ToWindowsColor(this Color color)
        {
            var windowsColor = new System.Windows.Media.Color();
            windowsColor.A = color.A;
            windowsColor.R = color.R;
            windowsColor.G = color.G;
            windowsColor.B = color.B;

            return windowsColor;
        }
    }
}
