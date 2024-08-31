using ArchSitePlan.UI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ArchSitePlan.UI.UserControls
{
    /// <summary>
    /// Interaction logic for HorizontalGraph.xaml
    /// </summary>
    public partial class HorizontalGraph : UserControl
    {
        // DependencyProperty for GraphData
        public static readonly DependencyProperty GraphDataProperty = DependencyProperty.Register(
                "GraphData",
                typeof(List<HorizontalGraphData>),
                typeof(HorizontalGraph),
                new PropertyMetadata(null));

        public static readonly DependencyProperty GraphTitleProperty = DependencyProperty.Register(
               "GraphTitle",
               typeof(string),
               typeof(HorizontalGraph),
               new PropertyMetadata(null));

        public List<HorizontalGraphData> _graphData = new();
        public List<HorizontalGraphData> GraphData
        {
            get { return (List<HorizontalGraphData>)GetValue(GraphDataProperty); }
            set { SetValue(GraphDataProperty, value); }
        }

        public string GraphTitle
        {
            get { return (string)GetValue(GraphTitleProperty); }
            set { SetValue(GraphTitleProperty, value); }
        }

        public HorizontalGraph()
        {
            InitializeComponent();
        }
    }
}
