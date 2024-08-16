using ArchSitePlan.UI.Constants;
using System.Drawing;
using System.Windows.Media;
using Color = System.Drawing.Color;

namespace ArchSitePlan.UI.Models
{
    public class HorizontalGraphData
    {
        public double Area { get; private set; }
        public double BarHeight { get; private set; }
        public string Name { get; private set; }
        public SolidColorBrush Color { get; private set; }
        public HorizontalGraphData(double area, string name, Color color, double maxArea)
        {
            Area = Math.Round(area,0);
            Name = name;
            Color = new SolidColorBrush(ToWindowsColor(color));
            BarHeight = SetBarHeight(Area, maxArea, GeneralConstants.MaxGraphBarSize);
        }

        private System.Windows.Media.Color ToWindowsColor(Color color)
        {
            var windowsColor = new System.Windows.Media.Color();
            windowsColor.A = color.A;
            windowsColor.R = color.R;
            windowsColor.G = color.G;
            windowsColor.B = color.B;

            return windowsColor;
        }

        private double SetBarHeight(double area, double maxArea, double maxGraphHeight)
        {
            return area / maxArea * maxGraphHeight;
        }
    }

    }
   
