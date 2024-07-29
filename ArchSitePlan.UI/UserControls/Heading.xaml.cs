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
    /// Interaction logic for Heading.xaml
    /// </summary>
    public partial class Heading : UserControl
    {
        public static readonly DependencyProperty HeadingTextProperty = DependencyProperty.Register(
                "Title",
                typeof(string),
                typeof(Heading),
                new PropertyMetadata(""));
        public string HeadingText
        {
            get { return (string)GetValue(HeadingTextProperty); }
            set { SetValue(HeadingTextProperty, value);}
        }

        

        public Heading()
        {
            InitializeComponent();
        }
    }
}
