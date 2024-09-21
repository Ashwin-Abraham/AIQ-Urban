using ArchSitePlan.UI.Constants;
using ArchSitePlan.UI.Extensions;
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
            Color = new SolidColorBrush(color.ToWindowsColor());
            BarHeight = SetBarHeight(Area, maxArea, RHGeneralConstants.MaxGraphBarSize);
        }

        private double SetBarHeight(double area, double maxArea, double maxGraphHeight)
        {
            return area / maxArea * maxGraphHeight;
        }
    }

    }
   
