using System;
using System.Collections.Generic;
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
    /// Interaction logic for MetricBox.xaml
    /// </summary>
    public partial class MetricBox : UserControl
    {
        // DependencyProperty for GraphData
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
                "Value",
                typeof(double),
                typeof(MetricBox),
                new PropertyMetadata(0.0));
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, Math.Round(value, 1)); }
        }

        public static readonly DependencyProperty UnitProperty = DependencyProperty.Register(
               "Unit",
               typeof(string),
               typeof(MetricBox),
               new PropertyMetadata(string.Empty));
        public string Unit
        {
            get { return (string)GetValue(UnitProperty); }
            set { SetValue(UnitProperty, value); }
        }

        public static readonly DependencyProperty MetricNameProperty = DependencyProperty.Register(
               "MetricName",
               typeof(string),
               typeof(MetricBox),
               new PropertyMetadata(string.Empty));

        public string MetricName
        {
            get { return (string)GetValue(MetricNameProperty); }
            set { SetValue(MetricNameProperty, value); }
        }

        public MetricBox()
        {
            InitializeComponent();
        }
    }
}
